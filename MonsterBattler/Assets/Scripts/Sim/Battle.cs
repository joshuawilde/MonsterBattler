using System.Collections.Generic;
using MonsterBattler.Sim.Data;
using MonsterBattler.Sim.Effects;
using MonsterBattler.Sim.Events;
using MonsterBattler.Sim.Log;

namespace MonsterBattler.Sim
{
    /// <summary>
    /// The battle engine.
    ///
    /// Turn loop (gen9 singles):
    ///   1. Both sides submit a <see cref="Choice"/> (move or switch).
    ///   2. Switches resolve first (in side order — TODO: pursuit-style interactions).
    ///   3. Moves resolve in priority / Speed order.
    ///   4. Each move runs the event pipeline (TryHit → accuracy → DamageCalc → Hit/DamagingHit
    ///      → move secondary → AfterMove).
    ///   5. End-of-turn: residuals fire for every active mon.
    ///   6. Auto-switch any fainted active slots from the bench.
    ///   7. Win condition: all team mons fainted on one side.
    /// </summary>
    public sealed partial class Battle
    {
        public readonly Dex Dex;
        public readonly Prng Prng;
        public readonly BattleLog Log = new();
        public readonly Field Field = new();
        public readonly Side[] Sides = new Side[2];

        public int TurnNumber;
        public bool IsFinished;
        public int? WinningSide;
        Pokemon _currentAttacker; // the mon currently using a move (for status source: Synchronize/Corrosion)
        bool _bouncing;           // guard so Magic Bounce can't ping-pong a status move forever
        bool _dancing;            // guard so Dancer can't recursively copy dance moves
        int _pivotPrefSide = -1;  // side whose Choice carried a pivot target this action…
        int _pivotPrefIdx = -1;   // …and the bench index it wants in (U-turn etc.)

        public Battle(Dex dex, ulong seed)
        {
            Dex = dex;
            Prng = new Prng(seed);
        }

        public void Setup(Side side0, Side side1)
        {
            Sides[0] = side0; side0.Index = 0;
            Sides[1] = side1; side1.Index = 1;
            foreach (var side in Sides)
                foreach (var mon in side.Team)
                    ResolveBaseEffects(mon);
            foreach (var side in Sides)
                if (side.ActiveSlots.Count > 0)
                {
                    side.ActiveSlots[0].HasBeenActive = true;
                    RunSwitchIn(new SwitchInEvent { Battle = this, Pokemon = side.ActiveSlots[0] });
                }
        }

        public void Step(Choice s0, Choice s1)
        {
            if (IsFinished) return;
            TurnNumber++;
            Log.Turn(TurnNumber);

            // Reset per-turn flags (Analytic = moved last, Stakeout = target switched in this turn).
            foreach (var side in Sides)
                foreach (var mon in side.Team) { mon.ActedThisTurn = false; mon.SwitchedInThisTurn = false; }

            // Phase 0: Pursuit intercept — a Pursuit user catches a switching foe with doubled BP.
            InterceptPursuit(ref s0, ref s1);

            // Phase 1: switches (both sides, side-0 first as a placeholder for richer ordering).
            if (s0.Kind == ChoiceKind.Switch) { TrySwitch(Sides[0], s0.SwitchToIndex); if (Sides[0].ActiveSlots.Count > 0) Sides[0].ActiveSlots[0].SwitchedInThisTurn = true; }
            if (s1.Kind == ChoiceKind.Switch) { TrySwitch(Sides[1], s1.SwitchToIndex); if (Sides[1].ActiveSlots.Count > 0) Sides[1].ActiveSlots[0].SwitchedInThisTurn = true; }

            // Phase 2: moves in priority/Speed order.
            var moveActions = new List<(Side attacker, Side defender, Choice c)>();
            if (s0.Kind == ChoiceKind.Move) moveActions.Add((Sides[0], Sides[1], s0));
            if (s1.Kind == ChoiceKind.Move) moveActions.Add((Sides[1], Sides[0], s1));
            OrderMoves(moveActions);
            foreach (var m in moveActions)
            {
                if (IsFinished) break;
                ResolveAction(m.attacker, m.defender, m.c);
            }
            if (IsFinished) return;

            // Phase 3a: per-mon residuals (status DoTs, leftovers, ability residuals…).
            foreach (var side in Sides)
                foreach (var mon in side.ActiveSlots)
                    if (!mon.IsFainted)
                        RunResidual(new ResidualEvent { Battle = this, Target = mon });

            // Phase 3b: weather chip damage + duration tick.
            TickWeather();

            // Phase 3c: side + field condition duration ticks (screens, Tailwind, Trick Room).
            TickSideConditions();
            TickFieldConditions();

            // Phase 3d: clean up single-turn volatiles (Protect / Detect / etc.).
            ClearSingleTurnVolatiles();

            // Phase 4: faint logs + auto-switch from bench. TODO: surface a forced-switch
            // decision to the player instead of auto-picking next alive.
            foreach (var side in Sides)
                foreach (var mon in side.ActiveSlots)
                    if (mon.IsFainted && !mon.FaintLogged)
                    {
                        mon.FaintLogged = true;
                        Log.Faint(mon);
                        var fe = new FaintEvent { Battle = this, Pokemon = mon, Source = mon.LastDamageSource };
                        // The fainting mon reacts (Aftermath), then every other active mon does
                        // (Moxie/Chilling-Grim Neigh check ev.Source==self; Soul-Heart on any faint).
                        Dispatch(mon, (e, o) => e.OnFaint(fe, o));
                        foreach (var s2 in Sides)
                            foreach (var other in s2.ActiveSlots)
                                if (other != null && other != mon && !other.IsFainted)
                                    Dispatch(other, (e, o) => e.OnFaint(fe, o));
                    }
            AutoSwitchFainted();

            CheckWinCondition();
        }

        /// <summary>Move priority after ability modifiers (Prankster/Gale Wings/Triage).</summary>
        int EffectivePriority(Pokemon user, Data.MoveData move)
        {
            if (user == null) return move.Priority;
            var ev = new ModifyPriorityEvent { Battle = this, User = user, Move = move, Priority = move.Priority };
            Dispatch(user, (e, o) => e.OnModifyPriority(ev, o));
            return ev.Priority;
        }

        void OrderMoves(List<(Side attacker, Side defender, Choice c)> moves)
        {
            if (moves.Count < 2) return;
            var a = moves[0]; var b = moves[1];
            var aMove = Dex.GetMove(a.c.MoveId);
            var bMove = Dex.GetMove(b.c.MoveId);
            int aPrio = EffectivePriority(a.attacker.ActiveSlots[0], aMove);
            int bPrio = EffectivePriority(b.attacker.ActiveSlots[0], bMove);
            if (aPrio != bPrio) { if (aPrio < bPrio) (moves[0], moves[1]) = (moves[1], moves[0]); return; }
            int aSpe = GetEffectiveSpeed(a.attacker.ActiveSlots[0]);
            int bSpe = GetEffectiveSpeed(b.attacker.ActiveSlots[0]);
            if (aSpe == bSpe) { if (Prng.Chance(1, 2)) (moves[0], moves[1]) = (moves[1], moves[0]); return; }
            bool trickRoom = Field.Conditions.ContainsKey("trickroom");
            // Trick Room flips the comparison: slower goes first.
            bool aGoesFirst = trickRoom ? aSpe < bSpe : aSpe > bSpe;
            if (!aGoesFirst) (moves[0], moves[1]) = (moves[1], moves[0]);
        }

        void InterceptPursuit(ref Choice s0, ref Choice s1)
        {
            Check(Sides[0], ref s0, ref s1, Sides[1]);
            Check(Sides[1], ref s1, ref s0, Sides[0]);

            void Check(Side switching, ref Choice switchChoice, ref Choice oppChoice, Side opposing)
            {
                if (switching == null || opposing == null) return;
                if (switchChoice.Kind != ChoiceKind.Switch) return;
                if (oppChoice.Kind != ChoiceKind.Move) return;
                if (oppChoice.MoveId != "pursuit") return;
                if (opposing.ActiveSlots.Count == 0 || switching.ActiveSlots.Count == 0) return;
                var pursuitUser = opposing.ActiveSlots[0];
                var pursuitTarget = switching.ActiveSlots[0];
                if (pursuitUser.IsFainted || pursuitTarget.IsFainted) return;
                pursuitUser.Tags.Add("pursuitintercept");
                UseMove(pursuitUser, pursuitTarget, "pursuit");
                pursuitUser.Tags.Remove("pursuitintercept");
                // Consume the Pursuit user's action so they don't try again in Phase 2.
                oppChoice = new Choice { Kind = ChoiceKind.Skip };
            }
        }

        void ResolveAction(Side attacker, Side defender, Choice choice)
        {
            if (choice.Kind == ChoiceKind.Skip) return;
            if (choice.Kind != ChoiceKind.Move) return;
            var user = attacker.ActiveSlots[0];
            if (user.IsFainted) return;
            user.ActedThisTurn = true; // for Analytic (did the foe already move this turn?)

            // Terastallize before the move — once per battle per side, requires a TeraType set.
            if (choice.Terastallize && !user.IsTerastallized && user.TeraType != MonType.None && !attacker.HasUsedTera)
            {
                user.IsTerastallized = true;
                attacker.HasUsedTera = true;
                Log.Raw($"|-terastallize|{Ident(user)}|{user.TeraType}");
            }

            var moveData = Dex.GetMove(choice.MoveId);
            // Self-targeting moves (Swords Dance, Roost, etc.) route back to the user.
            // Other targeting modes (spread, ally, field) collapse to "the opposing slot"
            // until the rest of the targeting system lands.
            var target = moveData.Target == MoveTarget.Self ? user : defender.ActiveSlots[0];
            if (target.IsFainted) return;
            // Register this side's pivot preference for the duration of the move (Magic Bounce
            // re-entry runs as the OTHER side's user, so the side check keeps it from leaking).
            _pivotPrefSide = attacker.Index;
            _pivotPrefIdx = choice.PivotToIndex;
            try { UseMove(user, target, choice.MoveId); }
            finally { _pivotPrefSide = -1; _pivotPrefIdx = -1; }
        }

        /// <summary>
        /// Swap the side's active slot with a bench mon. Fires OnSwitchOut/OnSwitchIn.
        /// </summary>
        /// <summary>Try to swap, blocked by trapping volatiles (Bind, Wrap, etc.). Logs and no-ops if trapped.</summary>
        public void TrySwitch(Side side, int newIndex)
        {
            if (side == null) return;
            var outgoing = side.ActiveSlots.Count > 0 ? side.ActiveSlots[0] : null;
            if (outgoing != null && outgoing.Volatiles.ContainsKey("partiallytrapped"))
            {
                Log.Raw($"|cant|{Ident(outgoing)}|partiallytrapped");
                return;
            }
            Switch(side, newIndex);
        }

        public void Switch(Side side, int newIndex)
        {
            if (newIndex < 0 || newIndex >= side.Team.Count) return;
            var incoming = side.Team[newIndex];
            if (incoming.IsFainted) return;
            var outgoing = side.ActiveSlots.Count > 0 ? side.ActiveSlots[0] : null;
            if (outgoing == incoming) return;

            if (outgoing != null)
            {
                RunSwitchOut(new SwitchOutEvent { Battle = this, Pokemon = outgoing });
                outgoing.IsActive = false;
                outgoing.TypeOverridden = false; outgoing.ItemLost = false; // revert on switch out
                outgoing.Volatiles.Clear();
                outgoing.Tags.Clear();
                outgoing.ToxicCounter = 0;
                outgoing.LastMoveUsed = null;
                outgoing.LockedMoveId = null;
                for (int i = 0; i < outgoing.StatStages.Length; i++) outgoing.StatStages[i] = 0;
            }

            if (side.ActiveSlots.Count > 0) side.ActiveSlots[0] = incoming;
            else side.ActiveSlots.Add(incoming);
            incoming.IsActive = true;
            incoming.HasBeenActive = true;
            Log.Raw($"|switch|{Ident(incoming)}|{incoming.Species?.Name}|{incoming.CurrentHp}/{incoming.MaxStats[(int)Stat.HP]}");
            RunSwitchIn(new SwitchInEvent { Battle = this, Pokemon = incoming });
        }

        /// <summary>PvP: both sides pick replacements through the session protocol, so the engine
        /// must not auto-switch anyone. Local battles leave this false (AI side auto-switches).</summary>
        public bool ManualSwitches;

        void AutoSwitchFainted()
        {
            if (ManualSwitches) return;
            foreach (var side in Sides)
            {
                // The player (side 0) picks their own replacement via the UI prompt.
                if (side.Index == 0) continue;
                if (side.ActiveSlots.Count == 0) continue;
                var active = side.ActiveSlots[0];
                if (!active.IsFainted) continue;
                for (int i = 0; i < side.Team.Count; i++)
                {
                    var candidate = side.Team[i];
                    if (candidate == active || candidate.IsFainted) continue;
                    Switch(side, i);
                    break;
                }
            }
        }

        void UseMove(Pokemon user, Pokemon target, string moveId)
        {
            // Mold Breaker / Teravolt / Turboblaze suppress the target's ability for the whole move;
            // Mycelium Might does the same, but only for the user's status moves.
            bool moldBreaker = user.AbilityEffect is Effects.Abilities.MoldBreakerEffect
                || user.AbilityEffect is Effects.Abilities.TeravoltEffect
                || user.AbilityEffect is Effects.Abilities.TurboblazeEffect
                || (user.AbilityEffect is Effects.Abilities.MyceliumMightEffect
                    && Dex.GetMove(moveId).Category == MoveCategory.Status);
            Effects.Effect suppressed = null;
            if (moldBreaker && target != null && target != user) { suppressed = target.AbilityEffect; target.AbilityEffect = null; }
            try { UseMoveCore(user, target, moveId); }
            finally { if (suppressed != null) target.AbilityEffect = suppressed; }

            // Gulp Missile: Cramorant enters its gulping form after Surf / Dive.
            if ((moveId == "surf" || moveId == "dive") && !user.IsFainted &&
                user.AbilityEffect is Effects.Abilities.GulpmissileEffect && user.GetVolatile("gulpmissilecharge") == null)
                AddVolatile(user, "gulpmissilecharge", user);

            // Dancer: any other active mon instantly copies a dance move.
            if (!_dancing && IsDanceMove(moveId))
            {
                foreach (var s in Sides)
                    foreach (var d in s.ActiveSlots)
                        if (d != null && d != user && !d.IsFainted && d.AbilityEffect is Effects.Abilities.DancerEffect)
                        {
                            _dancing = true;
                            var foeSide = OpposingSideOf(d);
                            var dTarget = foeSide != null && foeSide.ActiveSlots.Count > 0 ? foeSide.ActiveSlots[0] : user;
                            UseMove(d, dTarget, moveId);
                            _dancing = false;
                        }
            }
        }

        static bool IsPowderMove(string id) => id switch
        {
            "spore" or "sleeppowder" or "stunspore" or "poisonpowder" or "ragepowder"
            or "cottonspore" or "magicpowder" or "powder" => true,
            _ => false,
        };

        static bool IsDanceMove(string id) => id switch
        {
            "swordsdance" or "dragondance" or "quiverdance" or "victorydance" or "fierydance"
            or "revelationdance" or "petaldance" or "teeterdance" or "clangoroussoul" or "featherdance"
            or "lunardance" => true,
            _ => false,
        };

        /// <summary>True while a move is being invoked by another move (Sleep Talk) — lets it bypass the sleep gate.</summary>
        public bool CallingMove { get; private set; }

        /// <summary>Invoke a move without spending PP / choosing it (Sleep Talk, etc.).</summary>
        public void CallMove(Pokemon user, Pokemon target, string moveId)
        {
            bool prev = CallingMove;
            CallingMove = true;
            try { UseMoveCore(user, target, moveId); }
            finally { CallingMove = prev; }
        }

        void UseMoveCore(Pokemon user, Pokemon target, string moveId)
        {
            _currentAttacker = user; // status source for Synchronize / Corrosion
            var move = Dex.GetMove(moveId);

            // PP gate. Real PS substitutes Struggle when every slot is at 0; that escape hatch
            // comes later — for now an empty slot just fails the action.
            MoveSlot slot = null;
            for (int i = 0; i < user.Moves.Count; i++)
                if (user.Moves[i].Move.Id == moveId) { slot = user.Moves[i]; break; }
            if (slot != null && slot.Pp <= 0)
            {
                Log.Raw($"|cant|{Ident(user)}|nopp|{move.Name}");
                return;
            }
            if (slot != null) slot.Pp--;
            // Pressure: the target's ability makes the move cost an extra PP.
            if (slot != null && target != null && target != user && target.AbilityEffect is Effects.Abilities.PressureEffect)
                slot.Pp = System.Math.Max(0, slot.Pp - 1);

            // BeforeMove gate: Sleep, Confusion, full-paralyze, Truant, Recharge, etc.
            var before = new BeforeMoveEvent { Battle = this, User = user, Move = move };
            RunBeforeMove(before);
            if (before.Cancelled) return;

            // Protean / Libero: the user becomes the move's type the first time it acts each switch-in.
            if ((user.AbilityEffect is Effects.Abilities.ProteanEffect || user.AbilityEffect is Effects.Abilities.LiberoEffect)
                && !user.Tags.Contains("typechanged") && move.Type != MonType.None && !user.IsTerastallized)
            {
                user.TypeOverridden = true; user.OType1 = move.Type; user.OType2 = MonType.None;
                user.Tags.Add("typechanged");
                Log.Raw($"|-start|{Ident(user)}|typechange|[from] ability");
            }

            // Damp: blocks explosion-style moves while any active mon has it.
            if ((moveId == "explosion" || moveId == "selfdestruct" || moveId == "mistyexplosion" || moveId == "mindblown")
                && AnyActiveHas<Effects.Abilities.DampEffect>())
            {
                Log.Raw($"|cant|{Ident(user)}|ability: Damp|{move.Name}");
                return;
            }
            // Queenly Majesty / Dazzling / Armor Tail: block opposing increased-priority moves.
            if (target != null && target != user && EffectivePriority(user, move) > 0 &&
                target.AbilityEffect is Effects.Abilities.QueenlyMajestyEffect)
            {
                Log.Raw($"|cant|{Ident(target)}|ability: Queenly Majesty|{move.Name}");
                return;
            }
            // Magic Bounce: reflects an opposing status move back at its user.
            if (!_bouncing && target != null && target != user && move.Category == MoveCategory.Status &&
                move.Target != MoveTarget.Self && target.AbilityEffect is Effects.Abilities.MagicBounceEffect)
            {
                Log.Raw($"|-activate|{Ident(target)}|ability: Magic Bounce");
                _bouncing = true;
                UseMoveCore(target, user, moveId);
                _bouncing = false;
                return;
            }
            // Powder moves miss Grass types, Overcoat holders, and Safety Goggles.
            if (target != null && target != user && IsPowderMove(moveId) &&
                (HasType(target, MonType.Grass) || target.AbilityEffect is Effects.Abilities.OvercoatEffect || target.HasItem("safetygoggles")))
            {
                Log.Raw($"|-immune|{Ident(target)}");
                return;
            }

            // Two-turn charge moves (Solar Beam, Sky Attack, Phantom Force).
            if (move.TwoTurn)
            {
                bool alreadyCharging = user.Volatiles.ContainsKey("twoturncharge");
                if (!alreadyCharging)
                {
                    // Solar Beam fires same turn in Sun.
                    bool skipCharge = move.Id == "solarbeam" &&
                        (Field.Weather == Weather.Sun || Field.Weather == Weather.HarshSun);
                    if (!skipCharge)
                    {
                        AddVolatile(user, "twoturncharge", source: user);
                        user.LockedMoveId = move.Id;
                        Log.Raw($"|-prepare|{Ident(user)}|{move.Name}");
                        return;
                    }
                }
                else
                {
                    RemoveVolatile(user, "twoturncharge");
                    user.LockedMoveId = null;
                }
            }

            Log.Move(user, move, target);

            var tryHit = new TryHitEvent { Battle = this, User = user, Target = target, Move = move };
            RunTryHit(tryHit);
            if (tryHit.Blocked)
            {
                Log.Raw($"|-immune|{Ident(target)}|[from] ability: {tryHit.BlockReason}");
                return;
            }

            // Accuracy check with accuracy/evasion stages folded in, then modifier event hooks
            // (Compound Eyes, Sand Veil, Snow Cloak, etc.) get to tweak the threshold.
            if (move.Accuracy > 0)
            {
                int accDiff = user.StatStages[(int)Stat.Acc] - target.StatStages[(int)Stat.Eva];
                int threshold = (int)(move.Accuracy * Stats.AccuracyStageMult(accDiff));
                var accEv = new ModifyAccuracyEvent { Battle = this, User = user, Target = target, Move = move, Accuracy = threshold };
                RunModifyAccuracy(accEv);
                if (!Prng.Chance(System.Math.Max(1, accEv.Accuracy), 100))
                {
                    Log.Raw($"|-miss|{Ident(user)}|{Ident(target)}");
                    return;
                }
            }

            int totalDamage = 0;
            bool isCrit = false;
            int hitsConnected = 0;
            var moveEffect = EffectRegistry.Get(move.EffectId);

            if (move.Category != MoveCategory.Status && move.BasePower > 0)
            {
                int targetHits = RollMultihitCount(move);
                // Skill Link: multi-hit moves always hit the maximum number of times.
                if (user.AbilityEffect is Effects.Abilities.SkillLinkEffect && move.MultihitMax >= 2)
                    targetHits = move.MultihitMax;
                for (int h = 0; h < targetHits; h++)
                {
                    if (target.IsFainted) break;
                    isCrit = RollCrit(move.CritRatio)
                        && !(target.AbilityEffect is Effects.Abilities.ShellArmorEffect); // Shell/Battle Armor: never crit
                    int hitDamage = DamageCalc.Compute(this, user, target, move, isCrit);

                    // Disguise / Ice Face: the first qualifying hit is absorbed and breaks the form.
                    if (hitDamage > 0 && !target.Tags.Contains("formbroken"))
                    {
                        bool disguise = target.AbilityEffect is Effects.Abilities.DisguiseEffect;
                        bool iceFace = target.AbilityEffect is Effects.Abilities.IceFaceEffect && move.Category == MoveCategory.Physical;
                        if (disguise || iceFace)
                        {
                            target.Tags.Add("formbroken");
                            Log.Raw($"|-activate|{Ident(target)}|ability: {(disguise ? "Disguise" : "Ice Face")}");
                            if (disguise)
                            {
                                ApplyDamage(target, System.Math.Max(1, target.MaxStats[(int)Stat.HP] / 8), DamageSource.Other);
                                Log.Raw($"|-damage|{Ident(target)}|{target.CurrentHp}/{target.MaxStats[(int)Stat.HP]}");
                            }
                            continue; // hit fully absorbed
                        }
                    }

                    var sub = target.GetVolatile("substitute");
                    bool absorbed = sub != null && hitDamage > 0 && !move.Sound && user != target
                        && !(user.AbilityEffect is Effects.Abilities.InfiltratorEffect); // Infiltrator pierces Substitute
                    if (absorbed)
                    {
                        if (hitDamage >= sub.Counter) RemoveVolatile(target, "substitute");
                        else
                        {
                            sub.Counter -= hitDamage;
                            Log.Raw($"|-activate|{Ident(target)}|move: Substitute|[damage]");
                        }
                        hitDamage = 0;
                    }
                    else
                    {
                        ApplyDamage(target, hitDamage);
                        if (isCrit) Log.Raw($"|-crit|{Ident(target)}");
                        Log.Damage(target, hitDamage);
                        RecordIncomingDamage(target, hitDamage, move.Category, user, move.Contact);
                    }

                    var hit = new HitEvent { Battle = this, User = user, Target = target, Move = move, DamageDealt = hitDamage };
                    RunHit(hit);
                    if (hitDamage > 0) RunDamagingHit(hit);
                    if (moveEffect != null && hitDamage > 0) moveEffect.OnDamagingHit(hit, null);
                    if (moveEffect != null) moveEffect.OnHit(hit, null);

                    totalDamage += hitDamage;
                    hitsConnected++;
                }
                if (targetHits > 1) Log.Raw($"|-hitcount|{Ident(target)}|{hitsConnected}");
            }
            else
            {
                // Status / zero-BP moves still need OnHit dispatch for the effect.
                var hit = new HitEvent { Battle = this, User = user, Target = target, Move = move, DamageDealt = 0 };
                RunHit(hit);
                if (moveEffect != null) moveEffect.OnHit(hit, null);
            }

            // Flinch chance — applied to the target post-hit. Only meaningful if attacker outsped.
            if (move.FlinchChance > 0 && totalDamage > 0 && !target.IsFainted && Prng.Chance(move.FlinchChance, 100))
                AddVolatile(target, "flinch", singleTurn: true);

            // Chance-based secondaries (status / stat-drop / confusion / self-boost) and guaranteed
            // self stat changes. For damaging moves these only fire if at least one hit connected.
            bool connected = move.Category != MoveCategory.Status ? totalDamage > 0 : true;
            ApplySecondaries(user, target, move, connected);

            // Drain: heal user for a fraction of total damage dealt — unless Liquid Ooze hurts instead.
            if (move.DrainDen > 0 && totalDamage > 0 && !user.IsFainted)
            {
                int max = user.MaxStats[(int)Stat.HP];
                int heal = System.Math.Max(1, totalDamage * move.DrainNum / move.DrainDen);
                if (target != null && target.AbilityEffect is Effects.Abilities.LiquidOozeEffect)
                {
                    ApplyDamage(user, heal, DamageSource.Other);
                    Log.Raw($"|-damage|{Ident(user)}|{user.CurrentHp}/{max}|[from] ability: Liquid Ooze|[of] {Ident(target)}");
                }
                else
                {
                    int actual = System.Math.Min(heal, max - user.CurrentHp);
                    if (actual > 0)
                    {
                        user.CurrentHp += actual;
                        Log.Raw($"|-heal|{Ident(user)}|{user.CurrentHp}/{max}|[from] drain|[of] {Ident(target)}");
                    }
                }
            }

            // Recoil: user takes a fraction of total damage dealt (Rock Head negates it).
            if (move.RecoilDen > 0 && totalDamage > 0 && !user.IsFainted &&
                !(user.AbilityEffect is Effects.Abilities.RockHeadEffect))
            {
                int recoil = System.Math.Max(1, totalDamage * move.RecoilNum / move.RecoilDen);
                ApplyDamage(user, recoil, DamageSource.Recoil);
                Log.Raw($"|-damage|{Ident(user)}|{user.CurrentHp}/{user.MaxStats[(int)Stat.HP]}|[from] Recoil");
            }

            // Self-KO moves: bypass Magic Guard, so use Move source intentionally.
            if (move.SelfKO && !user.IsFainted)
            {
                ApplyDamage(user, user.CurrentHp, DamageSource.Move);
                Log.Faint(user);
            }

            // Last move tracking — only on successful moves (we got past PP and BeforeMove gates).
            user.LastMoveUsed = move;

            RunAfterMove(new AfterMoveEvent { Battle = this, User = user, Target = target, Move = move, DamageDealt = totalDamage });

            // Pivot moves (U-turn, Volt Switch, Flip Turn): the user switches out after the
            // move connects, provided they're still standing and have a non-fainted bench mon.
            // The acting side may have pre-chosen who comes in (Choice.PivotToIndex, threaded via
            // _pivotPrefSide/_pivotPrefIdx); otherwise auto-pick the first alive slot.
            if (move.PivotsOut && !user.IsFainted)
            {
                var side = SideOf(user);
                if (side != null)
                {
                    int want = side.Index == _pivotPrefSide ? _pivotPrefIdx : -1;
                    if (want >= 0 && want < side.Team.Count &&
                        side.Team[want] != user && !side.Team[want].IsFainted)
                    {
                        Switch(side, want);
                    }
                    else
                    {
                        for (int i = 0; i < side.Team.Count; i++)
                        {
                            var candidate = side.Team[i];
                            if (candidate == user || candidate.IsFainted) continue;
                            Switch(side, i);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Attach a volatile effect (Leech Seed, Protect, Confusion, etc.) to the target.
        /// No-op if the target already has a volatile with this id, or the EffectId is unknown.
        /// </summary>
        public VolatileSlot AddVolatile(Pokemon target, string id, Pokemon source = null, int turns = -1, bool singleTurn = false)
        {
            if (target == null || target.IsFainted) return null;
            if (target.Volatiles.ContainsKey(id)) return null;
            if (VolatileBlockedByAbility(target, id)) return null; // Oblivious/Own Tempo/Aroma Veil/Inner Focus
            var effect = EffectRegistry.Get(id);
            if (effect == null) return null;
            var slot = new VolatileSlot { Effect = effect, Source = source, Turns = turns, SingleTurn = singleTurn };
            target.Volatiles[id] = slot;
            Log.Raw($"|-start|{Ident(target)}|{id}");
            return slot;
        }

        /// <summary>Consume/remove a mon's held item (Berry eaten, Sash spent, Knock Off, …).
        /// Records it in LostItem, sets ItemLost, logs |-enditem|, and fires Cheek Pouch.</summary>
        public void ConsumeItem(Pokemon mon, string reason = null)
        {
            if (mon?.Item == null) return;
            var item = mon.Item;
            mon.LostItem = item;
            mon.Item = null; mon.ItemEffect = null; mon.ItemLost = true;
            Log.Raw($"|-enditem|{Ident(mon)}|{item.Name}{(reason != null ? $"|[from] {reason}" : "")}");
            // Cheek Pouch: eating a Berry also heals 1/3 max HP.
            if (item.IsBerry && mon.AbilityEffect is Effects.Abilities.CheekPouchEffect && !mon.IsFainted)
            {
                int max = mon.MaxStats[(int)Stat.HP];
                int heal = System.Math.Min(max / 3, max - mon.CurrentHp);
                if (heal > 0)
                {
                    mon.CurrentHp += heal;
                    Log.Raw($"|-heal|{Ident(mon)}|{mon.CurrentHp}/{max}|[from] ability: Cheek Pouch");
                }
            }
        }

        /// <summary>True if an opposing Unnerve (or As One) stops this mon from eating its Berry.</summary>
        public bool BerriesSuppressed(Pokemon mon)
        {
            var opp = OpposingSideOf(mon);
            if (opp == null) return false;
            foreach (var foe in opp.ActiveSlots)
                if (foe != null && !foe.IsFainted && (foe.AbilityEffect is Effects.Abilities.UnnerveEffect
                    || foe.AbilityEffect is Effects.Abilities.AsOneGlastrierEffect
                    || foe.AbilityEffect is Effects.Abilities.AsOneSpectrierEffect))
                    return true;
            return false;
        }

        /// <summary>A mon's battle weight in kg, after Heavy Metal (×2) / Light Metal (÷2) / Float Stone.</summary>
        public float EffectiveWeight(Pokemon mon)
        {
            if (mon?.Species == null) return 0f;
            float w = mon.Species.WeightKg;
            if (mon.AbilityEffect is Effects.Abilities.HeavymetalEffect) w *= 2f;
            else if (mon.AbilityEffect is Effects.Abilities.LightmetalEffect) w *= 0.5f;
            if (mon.HasItem("floatstone")) w *= 0.5f;
            return System.Math.Max(0.1f, w);
        }

        bool AnyActiveHas<T>() where T : Effects.Effect
        {
            foreach (var s in Sides)
                foreach (var m in s.ActiveSlots)
                    if (m != null && !m.IsFainted && m.AbilityEffect is T) return true;
            return false;
        }

        /// <summary>True if an opposing ability prevents this mon from voluntarily switching out.</summary>
        public bool IsTrapped(Pokemon mon)
        {
            if (mon == null || mon.IsFainted || mon.Species == null) return false;
            if (mon.Species.Type1 == MonType.Ghost || mon.Species.Type2 == MonType.Ghost) return false; // Ghosts never trapped
            var opp = OpposingSideOf(mon);
            if (opp == null) return false;
            foreach (var foe in opp.ActiveSlots)
            {
                if (foe == null || foe.IsFainted) continue;
                var ab = foe.AbilityEffect;
                if (ab is Effects.Abilities.ShadowTagEffect && !(mon.AbilityEffect is Effects.Abilities.ShadowTagEffect)) return true;
                if (ab is Effects.Abilities.MagnetPullEffect &&
                    (mon.Species.Type1 == MonType.Steel || mon.Species.Type2 == MonType.Steel)) return true;
                if (ab is Effects.Abilities.ArenaTrapEffect && IsGrounded(mon)) return true;
            }
            return false;
        }

        bool IsGrounded(Pokemon m)
        {
            if (m.Species != null && (m.Species.Type1 == MonType.Flying || m.Species.Type2 == MonType.Flying)) return false;
            if (m.AbilityEffect is Effects.Abilities.LevitateEffect) return false;
            if (m.HasItem("airballoon")) return false;
            return true;
        }

        // Abilities that grant immunity to specific volatile statuses.
        bool VolatileBlockedByAbility(Pokemon mon, string id)
        {
            var ab = mon.AbilityEffect;
            if (ab == null) return false;
            return ab switch
            {
                Effects.Abilities.ObliviousEffect => id == "taunt" || id == "attract",
                Effects.Abilities.OwnTempoEffect => id == "confusion",
                Effects.Abilities.AromaVeilEffect => id == "taunt" || id == "encore" || id == "disable" || id == "attract",
                _ => false,
            };
        }

        public bool RemoveVolatile(Pokemon target, string id)
        {
            if (target == null) return false;
            if (!target.Volatiles.Remove(id)) return false;
            Log.Raw($"|-end|{Ident(target)}|{id}");
            return true;
        }

        /// <summary>
        /// Add or stack a side condition (hazards, screens, Tailwind). Stacking layers up to
        /// <paramref name="maxLayers"/>; returns the resulting <see cref="SideCondition"/> or null
        /// if no further stacking was possible.
        /// </summary>
        public SideCondition AddSideCondition(Side side, string id, int maxLayers = 1, int turns = -1)
        {
            if (side == null) return null;
            if (side.Conditions.TryGetValue(id, out var existing))
            {
                if (existing.Layers >= maxLayers) return null;
                existing.Layers++;
                Log.Raw($"|-sidestart|p{side.Index + 1}|{id}");
                return existing;
            }
            var effect = EffectRegistry.Get(id);
            if (effect == null) return null;
            var c = new SideCondition { Id = id, Effect = effect, Layers = 1, TurnsLeft = turns };
            side.Conditions[id] = c;
            Log.Raw($"|-sidestart|p{side.Index + 1}|{id}");
            return c;
        }

        public bool RemoveSideCondition(Side side, string id)
        {
            if (side == null || !side.Conditions.Remove(id)) return false;
            Log.Raw($"|-sideend|p{side.Index + 1}|{id}");
            return true;
        }

        void ClearSingleTurnVolatiles()
        {
            foreach (var side in Sides)
            {
                if (side == null) continue;
                foreach (var mon in side.ActiveSlots)
                {
                    if (mon == null) continue;
                    var keys = new System.Collections.Generic.List<string>();
                    foreach (var kv in mon.Volatiles) if (kv.Value.SingleTurn) keys.Add(kv.Key);
                    foreach (var id in keys) mon.Volatiles.Remove(id);
                }
            }
        }

        /// <summary>
        /// Set or replace battlefield weather. Re-setting the same weather is a no-op (gen 6+
        /// behavior). Defaults to a 5-turn duration (8 with weather rocks — pass duration explicitly).
        /// </summary>
        public void SetWeather(Weather weather, int turns = 5)
        {
            if (Field.Weather == weather) return;
            Field.Weather = weather;
            Field.WeatherTurnsLeft = turns;
            Log.Raw($"|-weather|{weather}");
        }

        public void SetTerrain(Terrain terrain, int turns = 5)
        {
            if (Field.Terrain == terrain) return;
            Field.Terrain = terrain;
            Field.TerrainTurnsLeft = turns;
            Log.Raw($"|-fieldstart|{terrain} Terrain");
        }

        void TickWeather()
        {
            if (Field.Weather == Weather.None) return;

            // Sandstorm chip — Rock/Ground/Steel are immune. Ability-based immunities
            // (Sand Force, Sand Rush, Sand Veil, Magic Guard, Overcoat) land later.
            if (Field.Weather == Weather.Sandstorm)
            {
                foreach (var side in Sides)
                    foreach (var mon in side.ActiveSlots)
                    {
                        if (mon == null || mon.IsFainted) continue;
                        if (HasType(mon, MonType.Rock) || HasType(mon, MonType.Ground) || HasType(mon, MonType.Steel)) continue;
                        if (mon.AbilityEffect is Effects.Abilities.OvercoatEffect ||
                            mon.AbilityEffect is Effects.Abilities.MagicGuardEffect ||
                            mon.AbilityEffect is Effects.Abilities.SandForceEffect ||
                            mon.AbilityEffect is Effects.Abilities.SandRushEffect ||
                            mon.AbilityEffect is Effects.Abilities.SandVeilEffect ||
                            mon.HasItem("safetygoggles")) continue;
                        int dmg = System.Math.Max(1, mon.MaxStats[(int)Stat.HP] / 16);
                        ApplyDamage(mon, dmg, DamageSource.Sandstorm);
                        Log.Raw($"|-damage|{Ident(mon)}|{mon.CurrentHp}/{mon.MaxStats[(int)Stat.HP]}|[from] Sandstorm");
                    }
            }

            // Decrement and clear if expired.
            Field.WeatherTurnsLeft--;
            if (Field.WeatherTurnsLeft <= 0)
            {
                Log.Raw($"|-weather|none|[from] {Field.Weather}");
                Field.Weather = Weather.None;
                Field.WeatherTurnsLeft = 0;
            }
        }

        static bool HasType(Pokemon mon, MonType t)
        {
            if (mon?.Species == null) return false;
            return mon.Species.Type1 == t || mon.Species.Type2 == t;
        }

        /// <summary>Returns the side that owns <paramref name="mon"/>, or null if not on either team.</summary>
        public Side SideOf(Pokemon mon)
        {
            if (mon == null) return null;
            foreach (var s in Sides) if (s != null && s.Team.Contains(mon)) return s;
            return null;
        }

        /// <summary>Returns the side opposite to <paramref name="mon"/>'s side.</summary>
        public Side OpposingSideOf(Pokemon mon)
        {
            var s = SideOf(mon);
            if (s == null) return null;
            return Sides[1 - s.Index];
        }

        /// <summary>
        /// Pokemon's Speed after stat-stages + status (Paralysis) + items + abilities. Use this
        /// anywhere ordering depends on Speed (action queue, Trick Room flip, etc.).
        /// </summary>
        public int GetEffectiveSpeed(Pokemon mon)
        {
            if (mon == null) return 0;
            int spe = (int)(mon.MaxStats[(int)Stat.Spe] * Stats.StageMult(mon.StatStages[(int)Stat.Spe]));
            var ev = new StatModifyEvent { Battle = this, Owner = mon, Stat = Stat.Spe, Value = System.Math.Max(1, spe) };
            RunModifySpe(ev);
            return System.Math.Max(1, ev.Value);
        }

        /// <summary>
        /// Rolls hit count for a multi-hit move. Fixed if min==max; otherwise gen 5+ distribution
        /// for the canonical 2-5 spread: 2 (35%), 3 (35%), 4 (15%), 5 (15%).
        /// </summary>
        public int RollMultihitCount(Data.MoveData move)
        {
            if (move.MultihitMax <= 0) return 1;
            if (move.MultihitMin == move.MultihitMax) return move.MultihitMin;
            if (move.MultihitMin == 2 && move.MultihitMax == 5)
            {
                int r = Prng.Range(0, 100);
                if (r < 35) return 2;
                if (r < 70) return 3;
                if (r < 85) return 4;
                return 5;
            }
            return Prng.Range(move.MultihitMin, move.MultihitMax + 1);
        }

        /// <summary>Gen 6+ crit ratio table. Stage 3+ is guaranteed crit.</summary>
        public bool RollCrit(int critRatio)
        {
            if (critRatio >= 3) return true;
            return critRatio switch
            {
                1 => Prng.Chance(1, 8),
                2 => Prng.Chance(1, 2),
                _ => Prng.Chance(1, 24),
            };
        }

        /// <summary>
        /// Change a stat stage by <paramref name="delta"/> (clamped to ±6). Returns true on
        /// a meaningful change; emits a |-boost|/|-unboost|/|-fail| log line.
        /// </summary>
        public bool BoostStat(Pokemon mon, Stat stat, int delta, Pokemon source = null)
        {
            if (mon == null || mon.IsFainted || delta == 0) return false;
            // Contrary inverts every stat change before anything else sees it.
            if (mon.AbilityEffect is Effects.Abilities.ContraryEffect) delta = -delta;
            // Opponent-induced stat drops can be refused — or reflected — by ability.
            if (delta < 0 && source != null && source != mon && mon.AbilityEffect != null)
            {
                // Mirror Armor bounces the drop back at the attacker instead.
                if (mon.AbilityEffect is Effects.Abilities.MirrorArmorEffect)
                {
                    Log.Raw($"|-ability|{Ident(mon)}|Mirror Armor");
                    BoostStat(source, stat, delta, mon);
                    return false;
                }
                bool block = false;
                if (mon.AbilityEffect is Effects.Abilities.ClearBodyEffect ||
                    mon.AbilityEffect is Effects.Abilities.WhiteSmokeEffect ||
                    mon.AbilityEffect is Effects.Abilities.FullMetalBodyEffect) block = true;
                if (stat == Stat.Atk && mon.AbilityEffect is Effects.Abilities.HyperCutterEffect) block = true;
                if (stat == Stat.Def && mon.AbilityEffect is Effects.Abilities.BigPecksEffect) block = true;
                if (stat == Stat.Acc && mon.AbilityEffect is Effects.Abilities.KeenEyeEffect) block = true;
                if (block)
                {
                    Log.Raw($"|-immune|{Ident(mon)}|[from] ability: {mon.AbilityEffect.DisplayName}");
                    return false;
                }
            }
            int idx = (int)stat;
            int before = mon.StatStages[idx];
            int after = System.Math.Clamp(before + delta, -6, 6);
            if (before == after)
            {
                Log.Raw($"|-fail|{Ident(mon)}|stat: {stat}");
                return false;
            }
            mon.StatStages[idx] = after;
            int magnitude = System.Math.Abs(after - before);
            string verb = delta > 0 ? "boost" : "unboost";
            Log.Raw($"|-{verb}|{Ident(mon)}|{stat.ToString().ToLower()}|{magnitude}");
            // A foe lowered our stat — let reactive abilities answer (Defiant, Competitive).
            if (delta < 0 && source != null && source != mon)
            {
                var sl = new StatModifyEvent { Battle = this, Owner = mon, Stat = stat };
                Dispatch(mon, (e, o) => e.OnAfterStatLowered(sl, o));
            }
            return true;
        }

        /// <summary>
        /// Returns the effective weather: <see cref="Field"/> weather unless one of the active
        /// Pokemon has Cloud Nine / Air Lock, which negate weather effects globally.
        /// </summary>
        public Weather ActiveWeather()
        {
            foreach (var side in Sides)
                foreach (var mon in side.ActiveSlots)
                {
                    if (mon == null || mon.IsFainted) continue;
                    if (mon.AbilityEffect is Effects.Abilities.CloudNineEffect ||
                        mon.AbilityEffect is Effects.Abilities.AirLockEffect)
                        return Weather.None;
                }
            return Field.Weather;
        }

        public void ApplyDamage(Pokemon mon, int amount, DamageSource source = DamageSource.Move)
        {
            if (mon == null || mon.IsFainted || amount <= 0) return;
            // Magic Guard suppresses everything that isn't a direct move hit.
            if (source != DamageSource.Move && mon.AbilityEffect is Effects.Abilities.MagicGuardEffect) return;
            mon.CurrentHp = System.Math.Max(0, mon.CurrentHp - amount);
        }

        /// <summary>Tracks the last damage instance received — used by Counter / Mirror Coat / Metal Burst.</summary>
        public void RecordIncomingDamage(Pokemon target, int amount, MoveCategory category, Pokemon source, bool contact = false)
        {
            if (target == null || amount <= 0) return;
            target.LastDamageAmount = amount;
            target.LastDamageCategory = category;
            target.LastDamageSource = source;
            target.LastDamageTurn = TurnNumber;
            target.LastDamageWasContact = contact;
        }

        /// <summary>
        /// Swap a Pokemon's species in-place — used by Aegislash stance change, Wishiwashi schooling,
        /// Palafin Zero/Hero, and gen8/9 Dawn-Wing-Lunala-style form swaps. Re-computes max stats with
        /// neutral nature / 31 IVs / 0 EVs (matching DemoDex), preserves current HP value.
        /// </summary>
        public void ChangeForm(Pokemon mon, string newSpeciesId)
        {
            if (mon == null || !Dex.Species.TryGetValue(newSpeciesId, out var newSp)) return;
            mon.Species = newSp;
            int level = mon.Level;
            mon.MaxStats[(int)Stat.HP]  = (2 * newSp.BaseStats.HP  + 31) * level / 100 + level + 10;
            mon.MaxStats[(int)Stat.Atk] = (2 * newSp.BaseStats.Atk + 31) * level / 100 + 5;
            mon.MaxStats[(int)Stat.Def] = (2 * newSp.BaseStats.Def + 31) * level / 100 + 5;
            mon.MaxStats[(int)Stat.SpA] = (2 * newSp.BaseStats.SpA + 31) * level / 100 + 5;
            mon.MaxStats[(int)Stat.SpD] = (2 * newSp.BaseStats.SpD + 31) * level / 100 + 5;
            mon.MaxStats[(int)Stat.Spe] = (2 * newSp.BaseStats.Spe + 31) * level / 100 + 5;
            // Clamp current HP to new max (it may have shrunk).
            if (mon.CurrentHp > mon.MaxStats[(int)Stat.HP]) mon.CurrentHp = mon.MaxStats[(int)Stat.HP];
            Log.Raw($"|-formechange|{Ident(mon)}|{newSp.Name}");
        }

        /// <summary>Transform/Imposter: copy the target's species, stats (except HP), stages, types,
        /// ability and moves (each at 5 PP). The user keeps its own HP and max HP.</summary>
        public void Transform(Pokemon user, Pokemon target)
        {
            if (user == null || target == null || target.Species == null) return;
            user.Species = target.Species;
            for (int i = 1; i < 6; i++) user.MaxStats[i] = target.MaxStats[i]; // Atk..Spe; keep HP
            System.Array.Copy(target.StatStages, user.StatStages, target.StatStages.Length);
            var (t1, t2) = target.CurrentTypes();
            user.TypeOverridden = true; user.OType1 = t1; user.OType2 = t2;
            user.Ability = target.Ability; user.AbilityEffect = target.AbilityEffect;
            user.Moves.Clear();
            foreach (var ms in target.Moves)
                user.Moves.Add(new MoveSlot { Move = ms.Move, Pp = System.Math.Min(5, ms.MaxPp), MaxPp = System.Math.Min(5, ms.MaxPp) });
            user.Tags.Add("transformed");
            Log.Raw($"|-transform|{Ident(user)}|{Ident(target)}");
        }

        public void ApplyStatus(Pokemon target, StatusCondition status)
        {
            if (target.IsFainted || target.Status != StatusCondition.None) return;

            // Poison-type immunity to (bad) poison — bypassed by the attacker's Corrosion.
            bool corrosion = _currentAttacker != null && _currentAttacker.AbilityEffect is Effects.Abilities.CorrosionEffect;
            if ((status == StatusCondition.Poison || status == StatusCondition.BadlyPoisoned) && !corrosion &&
                target.Species != null && (target.Species.Type1 == MonType.Steel || target.Species.Type2 == MonType.Steel
                    || target.Species.Type1 == MonType.Poison || target.Species.Type2 == MonType.Poison))
            {
                Log.Raw($"|-immune|{Ident(target)}");
                return;
            }

            var tryEv = new TryStatusEvent { Battle = this, Target = target, Status = status };
            RunTryStatus(tryEv);
            if (tryEv.Blocked)
            {
                Log.Raw($"|-immune|{Ident(target)}|[from] ability: {tryEv.BlockReason}");
                return;
            }
            target.Status = status;
            target.StatusEffect = EffectRegistry.Get(StatusEffectId(status));
            if (status == StatusCondition.Sleep) target.SleepTurnsLeft = Prng.Range(1, 4); // gen 5+: 1..3 turns
            if (status == StatusCondition.BadlyPoisoned) target.ToxicCounter = 0;
            Log.Raw($"|-status|{Ident(target)}|{StatusEffectId(status)}");

            // Synchronize: reflect the status back at the attacker.
            if (target.AbilityEffect is Effects.Abilities.SynchronizeEffect && _currentAttacker != null && _currentAttacker != target &&
                (status == StatusCondition.Burn || status == StatusCondition.Poison ||
                 status == StatusCondition.BadlyPoisoned || status == StatusCondition.Paralysis))
            {
                ApplyStatus(_currentAttacker, status);
            }

            // Poison Puppeteer: a foe the owner poisons is also confused.
            if ((status == StatusCondition.Poison || status == StatusCondition.BadlyPoisoned) &&
                _currentAttacker != null && _currentAttacker != target &&
                _currentAttacker.AbilityEffect is Effects.Abilities.PoisonPuppeteerEffect)
            {
                AddVolatile(target, "confusion", _currentAttacker);
            }
        }

        /// <summary>
        /// Rolls and applies a move's chance-based secondaries plus its guaranteed self stat changes.
        /// Sheer Force suppresses all chance-based secondaries (its +BP is handled in DamageCalc);
        /// Serene Grace doubles their chance; Shield Dust blocks the target-facing parts.
        /// </summary>
        void ApplySecondaries(Pokemon user, Pokemon target, Data.MoveData move, bool connected)
        {
            if (!connected || user == null) return;

            bool sheerForce = user.AbilityEffect is Effects.Abilities.SheerForceEffect;
            if (move.Secondaries != null && !sheerForce)
            {
                bool serene = user.AbilityEffect is Effects.Abilities.SereneGraceEffect;
                bool shieldDust = target != null && target.AbilityEffect is Effects.Abilities.ShieldDustEffect;
                foreach (var sec in move.Secondaries)
                {
                    int chance = sec.Chance <= 0 ? 100 : (serene ? System.Math.Min(100, sec.Chance * 2) : sec.Chance);
                    if (!Prng.Chance(chance, 100)) continue;

                    // Self-boosts (e.g. Charge Beam) hit the user and ignore Shield Dust.
                    if (sec.SelfBoosts != null && !user.IsFainted)
                        foreach (var sc in sec.SelfBoosts) BoostStat(user, sc.Stat, sc.Delta, user);

                    if (shieldDust || target == null || target.IsFainted) continue;
                    if (!string.IsNullOrEmpty(sec.Status)) ApplyStatus(target, ParseStatus(sec.Status));
                    if (!string.IsNullOrEmpty(sec.Volatile))
                    {
                        var v = AddVolatile(target, sec.Volatile);
                        if (v != null && sec.Volatile == "confusion") v.Turns = Prng.Range(2, 6);
                    }
                    if (sec.TargetBoosts != null)
                        foreach (var sc in sec.TargetBoosts) BoostStat(target, sc.Stat, sc.Delta, user);
                }
            }

            // Guaranteed self stat changes (Close Combat, Overheat, …) — not a secondary, so Sheer
            // Force and Shield Dust never touch them.
            if (move.SelfBoosts != null && !user.IsFainted)
                foreach (var sc in move.SelfBoosts) BoostStat(user, sc.Stat, sc.Delta, user);
        }

        static StatusCondition ParseStatus(string code) => code switch
        {
            "brn" => StatusCondition.Burn,
            "par" => StatusCondition.Paralysis,
            "psn" => StatusCondition.Poison,
            "tox" => StatusCondition.BadlyPoisoned,
            "slp" => StatusCondition.Sleep,
            "frz" => StatusCondition.Freeze,
            "frb" or "fbt" or "frostbite" => StatusCondition.Frostbite,
            _ => StatusCondition.None,
        };

        static string StatusEffectId(StatusCondition s) => s switch
        {
            StatusCondition.Burn => "brn",
            StatusCondition.Paralysis => "par",
            StatusCondition.Poison => "psn",
            StatusCondition.BadlyPoisoned => "tox",
            StatusCondition.Sleep => "slp",
            StatusCondition.Freeze => "frz",
            StatusCondition.Frostbite => "frb",
            _ => null,
        };

        void ResolveBaseEffects(Pokemon mon)
        {
            mon.AbilityEffect = mon.Ability != null ? EffectRegistry.Get(mon.Ability.EffectId ?? mon.Ability.Id) : null;
            mon.ItemEffect = mon.Item != null ? EffectRegistry.Get(mon.Item.EffectId ?? mon.Item.Id) : null;
            mon.StatusEffect = mon.Status != StatusCondition.None ? EffectRegistry.Get(StatusEffectId(mon.Status)) : null;
        }

        void CheckWinCondition()
        {
            bool s0Out = Sides[0].Team.TrueForAll(p => p.IsFainted);
            bool s1Out = Sides[1].Team.TrueForAll(p => p.IsFainted);
            if (s0Out && s1Out) { IsFinished = true; WinningSide = null; }
            else if (s1Out) { IsFinished = true; WinningSide = 0; }
            else if (s0Out) { IsFinished = true; WinningSide = 1; }
        }

        public void RunBeforeMove(BeforeMoveEvent ev) { Dispatch(ev.User, (e, o) => e.OnBeforeMove(ev, o)); }
        public void RunTryHit(TryHitEvent ev)        { Dispatch(ev.Target, (e, o) => e.OnTryHit(ev, o)); }
        public void RunHit(HitEvent ev)              { Dispatch(ev.User, (e, o) => e.OnHit(ev, o)); Dispatch(ev.Target, (e, o) => e.OnHit(ev, o)); }
        public void RunDamagingHit(HitEvent ev)
        {
            // Walk user (Life Orb recoil, contact-with-recoil items) AND target (Static, Rough Skin, etc.).
            Dispatch(ev.User, (e, o) => e.OnDamagingHit(ev, o));
            Dispatch(ev.Target, (e, o) => e.OnDamagingHit(ev, o));
        }
        public void RunAfterMove(AfterMoveEvent ev)  { Dispatch(ev.User, (e, o) => e.OnAfterMove(ev, o)); }

        public void RunBasePower(BasePowerEvent ev)  { Dispatch(ev.User, (e, o) => e.OnBasePower(ev, o)); Dispatch(ev.Target, (e, o) => e.OnBasePower(ev, o)); }
        public void RunModifyType(ModifyTypeEvent ev) { Dispatch(ev.User, (e, o) => e.OnModifyType(ev, o)); }
        public void RunModifyAtk(StatModifyEvent ev) { Dispatch(ev.Owner, (e, o) => e.OnModifyAtk(ev, o)); }
        public void RunModifyDef(StatModifyEvent ev) { Dispatch(ev.Owner, (e, o) => e.OnModifyDef(ev, o)); }
        public void RunModifySpA(StatModifyEvent ev) { Dispatch(ev.Owner, (e, o) => e.OnModifySpA(ev, o)); }
        public void RunModifySpD(StatModifyEvent ev) { Dispatch(ev.Owner, (e, o) => e.OnModifySpD(ev, o)); }
        public void RunModifySpe(StatModifyEvent ev)
        {
            Dispatch(ev.Owner, (e, o) => e.OnModifySpe(ev, o));
            DispatchSideOf(ev.Owner, (e, o) => e.OnModifySpe(ev, o));
        }
        public void RunModifyDamage(ModifyDamageEvent ev)
        {
            Dispatch(ev.User, (e, o) => e.OnModifyDamage(ev, o));
            Dispatch(ev.Target, (e, o) => e.OnModifyDamage(ev, o));
            DispatchSideOf(ev.User, (e, o) => e.OnModifyDamage(ev, o));
            DispatchSideOf(ev.Target, (e, o) => e.OnModifyDamage(ev, o));
        }
        public void RunModifyAccuracy(ModifyAccuracyEvent ev)
        {
            Dispatch(ev.User, (e, o) => e.OnModifyAccuracy(ev, o));
            Dispatch(ev.Target, (e, o) => e.OnModifyAccuracy(ev, o));
        }

        void DispatchSideOf(Pokemon scope, System.Action<Effect, Pokemon> visit)
        {
            var side = SideOf(scope);
            if (side == null) return;
            foreach (var cond in new System.Collections.Generic.List<SideCondition>(side.Conditions.Values))
                if (cond.Effect != null) visit(cond.Effect, scope);
        }

        void TickFieldConditions()
        {
            if (Field.Conditions.Count == 0) return;
            var toRemove = new System.Collections.Generic.List<string>();
            foreach (var kv in Field.Conditions)
            {
                var c = kv.Value;
                if (c.TurnsLeft <= 0) continue;
                c.TurnsLeft--;
                if (c.TurnsLeft <= 0) toRemove.Add(kv.Key);
            }
            foreach (var id in toRemove)
            {
                Field.Conditions.Remove(id);
                Log.Raw($"|-fieldend|{id}");
            }
        }

        void TickSideConditions()
        {
            foreach (var side in Sides)
            {
                if (side == null) continue;
                var toRemove = new System.Collections.Generic.List<string>();
                foreach (var kv in side.Conditions)
                {
                    var c = kv.Value;
                    if (c.TurnsLeft <= 0) continue;
                    c.TurnsLeft--;
                    if (c.TurnsLeft <= 0) toRemove.Add(kv.Key);
                }
                foreach (var id in toRemove) RemoveSideCondition(side, id);
            }
        }

        public void RunSwitchIn(SwitchInEvent ev)
        {
            Dispatch(ev.Pokemon, (e, o) => e.OnSwitchIn(ev, o));
            // Hazards live on the side the incoming Pokemon belongs to.
            var side = SideOf(ev.Pokemon);
            if (side != null)
                // Snapshot: a hazard may remove itself on switch-in (Toxic Spikes absorbed by a
                // Poison type), which would invalidate a live enumerator over the conditions.
                foreach (var cond in new System.Collections.Generic.List<SideCondition>(side.Conditions.Values))
                    cond.Effect?.OnSwitchIn(ev, ev.Pokemon);
        }
        public void RunSwitchOut(SwitchOutEvent ev)  { Dispatch(ev.Pokemon, (e, o) => e.OnSwitchOut(ev, o)); }
        public void RunResidual(ResidualEvent ev)
        {
            Dispatch(ev.Target, (e, o) => e.OnResidual(ev, o));
            // Side residuals (Wish, future Light Screen flicker, etc.) fire for the active mon.
            DispatchSideOf(ev.Target, (e, o) => e.OnResidual(ev, o));
        }
        public void RunTryStatus(TryStatusEvent ev)
        {
            Dispatch(ev.Target, (e, o) => e.OnTryStatus(ev, o));
            // Safeguard lives as a side condition — let it block status applications.
            DispatchSideOf(ev.Target, (e, o) => e.OnTryStatus(ev, o));
        }

        static void Dispatch(Pokemon scope, System.Action<Effect, Pokemon> visit)
        {
            if (scope == null) return;
            // Snapshot first: a handler may add/remove a volatile (Confusion/Disable/Encore
            // expiring, Substitute breaking, …), which would invalidate a live enumerator.
            var effects = new System.Collections.Generic.List<Effect>(scope.ActiveEffects());
            foreach (var e in effects) visit(e, scope);
        }

        static string Ident(Pokemon mon) => mon?.Nickname ?? mon?.Species?.Name ?? "?";
    }

    public enum ChoiceKind { Move, Switch, Skip }

    public struct Choice
    {
        public ChoiceKind Kind;
        public string MoveId;
        public int SwitchToIndex;
        public bool Terastallize;
        /// <summary>For pivot moves (U-turn etc.): the bench index the user wants to bring in.
        /// 0 means unset (treated as -1 via PivotToIndex); the engine auto-picks when unset.</summary>
        public int PivotToIndexPlusOne;
        public readonly int PivotToIndex => PivotToIndexPlusOne - 1;
        public static Choice UseMove(string id, bool tera = false, int pivotTo = -1)
            => new Choice { Kind = ChoiceKind.Move, MoveId = id, Terastallize = tera, PivotToIndexPlusOne = pivotTo + 1 };
        public static Choice SwitchTo(int idx) => new Choice { Kind = ChoiceKind.Switch, SwitchToIndex = idx };
    }
}
