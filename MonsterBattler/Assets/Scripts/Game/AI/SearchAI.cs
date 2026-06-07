using System;
using System.Collections.Generic;
using MonsterBattler.Sim;

namespace MonsterBattler.Game.AI
{
    /// <summary>
    /// Eval-guided depth-D expectiminimax over <see cref="Battle.Clone"/>. Each ply: for my top-K
    /// candidate actions × the opponent's top-J responses, clone the battle, play both choices (and
    /// bring in replacements for any fainted active), then either recurse another ply or evaluate the
    /// position — averaging over a few random samples. I maximize, the opponent minimizes (minimax).
    /// depth=1 ≈ "play the move with the best immediate outcome"; depth≥2 looks several turns ahead
    /// (KO sequences, setup payoff, forced switches). Branching is pruned by the heuristic ordering.
    /// </summary>
    public sealed class SearchAI : IBattleAI
    {
        readonly int _depth, _topMy, _topOpp, _samples;
        readonly Prng _prng;

        public SearchAI(int depth = 2, int topMy = 4, int topOpp = 3, int samples = 3, ulong? seed = null)
        {
            _depth = Math.Max(1, depth);
            _topMy = topMy; _topOpp = topOpp; _samples = samples;
            _prng = seed.HasValue ? new Prng(seed.Value) : null;
        }

        public Choice ChooseAction(Battle battle, Side ownSide, Side opponentSide)
        {
            int me = ownSide.Index;
            var myActions = TopK(HeuristicEvaluator.Score(battle, ownSide, opponentSide), _topMy);
            if (myActions.Count == 0) return Choice.UseMove("tackle");

            var rng = _prng ?? battle.Prng;
            ulong seed = ((ulong)(uint)rng.Range(1, int.MaxValue) << 21) ^ (ulong)rng.Range(1, int.MaxValue);

            Choice best = myActions[0].Choice;
            float bestVal = float.NegativeInfinity;
            foreach (var ma in myActions)
            {
                float v = MinOverOpp(battle, me, ma.Choice, _depth, ref seed);
                if (v > bestVal) { bestVal = v; best = ma.Choice; }
            }
            return best;
        }

        // Value to `me` of committing to myChoice this ply (opponent then minimizes), recursing depth-1.
        float MinOverOpp(Battle b, int me, Choice myChoice, int depth, ref ulong seed)
        {
            var oppSide = b.Sides[1 - me];
            var oppActions = TopK(HeuristicEvaluator.Score(b, oppSide, b.Sides[me]), _topOpp);
            if (oppActions.Count == 0) oppActions.Add(new ActionScore { Choice = Choice.UseMove("tackle") });

            int samples = depth == _depth ? _samples : Math.Max(1, _samples - 1); // cheaper deeper
            float worst = float.PositiveInfinity;
            foreach (var oa in oppActions)
            {
                float sum = 0f;
                for (int s = 0; s < samples; s++)
                {
                    seed = seed * 6364136223846793005UL + 1442695040888963407UL; // LCG advance
                    var sim = b.Clone(seed);
                    Choice c0 = me == 0 ? myChoice : oa.Choice;
                    Choice c1 = me == 0 ? oa.Choice : myChoice;
                    sim.Step(c0, c1);
                    sum += Value(sim, me, depth - 1, ref seed);
                }
                float avg = sum / samples;
                if (avg < worst) worst = avg;
            }
            return worst;
        }

        // Value of a position to `me`, with `depth` plies left.
        float Value(Battle b, int me, int depth, ref ulong seed)
        {
            if (b.IsFinished || depth <= 0) return PositionEval(b, me);
            ResolveForcedSwitches(b); // replace fainted actives before the next ply
            if (b.IsFinished) return PositionEval(b, me);

            var myActions = TopK(HeuristicEvaluator.Score(b, b.Sides[me], b.Sides[1 - me]), _topMy);
            if (myActions.Count == 0) return PositionEval(b, me);

            float best = float.NegativeInfinity;
            foreach (var ma in myActions)
            {
                float v = MinOverOpp(b, me, ma.Choice, depth, ref seed);
                if (v > best) best = v;
            }
            return best;
        }

        // Bring in the best replacement for any side whose active has fainted (mirrors a forced switch).
        static void ResolveForcedSwitches(Battle b)
        {
            foreach (var side in b.Sides)
            {
                if (side.ActiveSlots.Count == 0) continue;
                var active = side.ActiveSlots[0];
                if (active == null || !active.IsFainted) continue;
                var foe = b.Sides[1 - side.Index].ActiveSlots.Count > 0 ? b.Sides[1 - side.Index].ActiveSlots[0] : null;
                int idx = BestSwitchIndex(side, foe);
                if (idx >= 0) b.Switch(side, idx);
            }
        }

        static int BestSwitchIndex(Side side, Pokemon foe)
        {
            int best = -1; float bestThreat = float.PositiveInfinity; int bestHp = -1;
            var active = side.ActiveSlots.Count > 0 ? side.ActiveSlots[0] : null;
            for (int i = 0; i < side.Team.Count; i++)
            {
                var p = side.Team[i];
                if (p == null || p == active || p.IsFainted) continue;
                float threat = foe == null ? 1f : WorstIncoming(foe, p);
                if (threat < bestThreat || (threat == bestThreat && p.CurrentHp > bestHp))
                { bestThreat = threat; bestHp = p.CurrentHp; best = i; }
            }
            return best;
        }

        static float WorstIncoming(Pokemon attacker, Pokemon defender)
        {
            var (d1, d2) = attacker.IsTerastallized ? (attacker.TeraType, MonType.None) : (attacker.Species.Type1, attacker.Species.Type2);
            float worst = 0f;
            var (e1, e2) = defender.IsTerastallized ? (defender.TeraType, MonType.None) : (defender.Species.Type1, defender.Species.Type2);
            if (d1 != MonType.None) worst = Math.Max(worst, TypeChart.Effectiveness(d1, e1, e2));
            if (d2 != MonType.None) worst = Math.Max(worst, TypeChart.Effectiveness(d2, e1, e2));
            return worst <= 0f ? 1f : worst;
        }

        static List<ActionScore> TopK(List<ActionScore> scored, int k)
        {
            scored.Sort((a, b) => b.Value.CompareTo(a.Value));
            if (scored.Count > k) scored.RemoveRange(k, scored.Count - k);
            return scored;
        }

        /// <summary>
        /// Position value from <paramref name="me"/>'s perspective. HP + alive count dominate; on top
        /// of that it weighs the active type matchup, stat boosts on the actives, entry hazards, screens
        /// and status severity — so the search prefers positions that are winning, not just even on HP.
        /// </summary>
        public static float PositionEval(Battle b, int me)
        {
            int opp = 1 - me;
            if (b.IsFinished)
            {
                if (b.WinningSide == me) return 1000f;
                if (b.WinningSide == opp) return -1000f;
                return 0f;
            }
            var mySide = b.Sides[me];
            var oppSide = b.Sides[opp];
            return SideScore(mySide) - SideScore(oppSide);
        }

        static Pokemon Active(Side s) =>
            s.ActiveSlots.Count > 0 && s.ActiveSlots[0] != null && !s.ActiveSlots[0].IsFainted ? s.ActiveSlots[0] : null;

        static float SideScore(Side s)
        {
            float total = 0f;
            foreach (var m in s.Team)
            {
                if (m == null) continue;
                int max = m.MaxStats[(int)Stat.HP];
                total += max > 0 ? (float)m.CurrentHp / max : 0f; // hp fraction dominates
                if (!m.IsFainted)
                {
                    total += 0.15f;                       // a live mon is worth more than its hp
                    total -= StatusPenalty(m.Status);
                }
            }
            var a = Active(s);
            if (a != null) total += BoostScore(a);        // boosts only matter on the field
            total -= HazardPenalty(s);                     // hazards on YOUR side chip your switch-ins
            total += ScreenBonus(s);
            return total;
        }

        static float BoostScore(Pokemon m)
        {
            float v = 0f;
            v += 0.05f * (m.StatStages[(int)Stat.Atk] + m.StatStages[(int)Stat.SpA]);
            v += 0.035f * (m.StatStages[(int)Stat.Def] + m.StatStages[(int)Stat.SpD]);
            v += 0.04f * m.StatStages[(int)Stat.Spe];
            return v;
        }

        static float HazardPenalty(Side s)
        {
            float p = 0f;
            if (s.Conditions.ContainsKey("stealthrock")) p += 0.12f;
            if (s.Conditions.TryGetValue("spikes", out var sp)) p += 0.05f * System.Math.Max(1, sp.Layers);
            if (s.Conditions.TryGetValue("toxicspikes", out var ts)) p += 0.04f * System.Math.Max(1, ts.Layers);
            if (s.Conditions.ContainsKey("stickyweb")) p += 0.03f;
            return p;
        }

        static float ScreenBonus(Side s)
        {
            float v = 0f;
            if (s.Conditions.ContainsKey("reflect")) v += 0.06f;
            if (s.Conditions.ContainsKey("lightscreen")) v += 0.06f;
            if (s.Conditions.ContainsKey("tailwind")) v += 0.05f;
            return v;
        }

        static float StatusPenalty(StatusCondition st) => st switch
        {
            StatusCondition.Sleep => 0.12f,
            StatusCondition.Freeze => 0.12f,
            StatusCondition.Burn => 0.08f,
            StatusCondition.BadlyPoisoned => 0.08f,
            StatusCondition.Frostbite => 0.08f,
            StatusCondition.Paralysis => 0.07f,
            StatusCondition.Poison => 0.05f,
            _ => 0f,
        };

    }
}
