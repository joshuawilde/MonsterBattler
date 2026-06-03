using System;
using MonsterBattler.Sim.Data;
using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim
{
    /// <summary>
    /// Mainline gen9 damage formula with event hooks woven in. Order of operations:
    ///   1. BasePower starts at MoveData.BasePower; RunBasePower lets abilities/items scale it.
    ///   2. Atk and Def use the user/target's MaxStats × stat-stage multiplier. On a crit the
    ///      attacker ignores its own negative Atk stages and the target ignores its positive
    ///      Def stages (PS's "ignore stages that hurt the attacker").
    ///   3. Stat-modifier events (RunModifyAtk/Def/SpA/SpD) run on the stage-adjusted value.
    ///   4. damage = floor(floor((2*L/5 + 2) * BP * A/D) / 50) + 2
    ///   5. Random roll (85..100 / 100).
    ///   6. STAB (× 1.5).
    ///   7. Type effectiveness.
    ///   8. Crit (× 1.5 in gen 6+).
    ///   9. RunModifyDamage — Life Orb, screens, multiscale, etc.
    /// </summary>
    public static class DamageCalc
    {
        public static int Compute(Battle battle, Pokemon user, Pokemon target, MoveData move, bool isCrit = false)
        {
            if (move.Category == MoveCategory.Status || move.BasePower <= 0) return 0;

            var bpEv = new BasePowerEvent { Battle = battle, User = user, Target = target, Move = move, BasePower = move.BasePower };
            battle.RunBasePower(bpEv);
            // The move's own effect also gets to tweak base power (Knock Off ×1.5 on item, etc.).
            var moveEffect = Effects.EffectRegistry.Get(move.EffectId);
            if (moveEffect != null) moveEffect.OnBasePower(bpEv, null);
            int basePower = Math.Max(1, bpEv.BasePower);

            var atkStatKind = move.Category == MoveCategory.Physical ? Stat.Atk : Stat.SpA;
            var defStatKind = move.Category == MoveCategory.Physical ? Stat.Def : Stat.SpD;

            // Stat stages — crit ignores stages that hurt the attacker.
            int atkStage = user.StatStages[(int)atkStatKind];
            int defStage = target.StatStages[(int)defStatKind];
            if (isCrit)
            {
                if (atkStage < 0) atkStage = 0;
                if (defStage > 0) defStage = 0;
            }
            int atkStat = Math.Max(1, (int)(user.MaxStats[(int)atkStatKind] * Stats.StageMult(atkStage)));
            int defStat = Math.Max(1, (int)(target.MaxStats[(int)defStatKind] * Stats.StageMult(defStage)));

            var atkEv = new StatModifyEvent { Battle = battle, Owner = user, Stat = atkStatKind, Value = atkStat, ContextMove = move };
            if (atkStatKind == Stat.Atk) battle.RunModifyAtk(atkEv); else battle.RunModifySpA(atkEv);
            atkStat = Math.Max(1, atkEv.Value);

            var defEv = new StatModifyEvent { Battle = battle, Owner = target, Stat = defStatKind, Value = defStat, ContextMove = move };
            if (defStatKind == Stat.Def) battle.RunModifyDef(defEv); else battle.RunModifySpD(defEv);
            defStat = Math.Max(1, defEv.Value);

            // Weather-based defensive boosts (Cloud Nine / Air Lock suppresses).
            var weather = battle.ActiveWeather();
            if (weather == Weather.Sandstorm && defStatKind == Stat.SpD && IsType(target, MonType.Rock))
                defStat = defStat * 3 / 2;
            if (weather == Weather.Snow && defStatKind == Stat.Def && IsType(target, MonType.Ice))
                defStat = defStat * 3 / 2;

            int level = user.Level;
            int dmg = (int)Math.Floor((2.0 * level / 5.0 + 2.0) * basePower * atkStat / defStat);
            dmg = (int)Math.Floor(dmg / 50.0) + 2;

            int roll = battle.Prng.Range(85, 101);
            dmg = dmg * roll / 100;

            // STAB (gen 9 Tera rules):
            //   • match original type only             → ×1.5
            //   • match Tera type only (post-tera)     → ×1.5
            //   • match both (post-tera)               → ×2.0
            bool originalStab = user.Species != null &&
                (move.Type == user.Species.Type1 || move.Type == user.Species.Type2);
            bool teraStab = user.IsTerastallized && move.Type == user.TeraType;
            if (originalStab && teraStab) dmg = dmg * 2;
            else if (originalStab || teraStab) dmg = dmg * 3 / 2;

            // Defensive types: Terastallization replaces the defender's types with TeraType.
            MonType defType1 = target.IsTerastallized ? target.TeraType : (target.Species?.Type1 ?? MonType.None);
            MonType defType2 = target.IsTerastallized ? MonType.None : (target.Species?.Type2 ?? MonType.None);
            float eff = TypeChart.Effectiveness(move.Type, defType1, defType2);
            dmg = (int)(dmg * eff);

            // Weather damage multipliers (after STAB/type, before crit). Cloud Nine / Air Lock
            // suppresses weather effects globally via ActiveWeather().
            switch (battle.ActiveWeather())
            {
                case Weather.Sun:
                    if (move.Type == MonType.Fire) dmg = dmg * 3 / 2;
                    else if (move.Type == MonType.Water) dmg = dmg / 2;
                    break;
                case Weather.Rain:
                    if (move.Type == MonType.Water) dmg = dmg * 3 / 2;
                    else if (move.Type == MonType.Fire) dmg = dmg / 2;
                    break;
                case Weather.HarshSun:
                    if (move.Type == MonType.Water) return 0; // Water moves fail in extremely harsh sunlight.
                    if (move.Type == MonType.Fire) dmg = dmg * 3 / 2;
                    break;
                case Weather.HeavyRain:
                    if (move.Type == MonType.Fire) return 0;
                    if (move.Type == MonType.Water) dmg = dmg * 3 / 2;
                    break;
            }

            if (isCrit) dmg = dmg * 3 / 2;

            var modEv = new ModifyDamageEvent { Battle = battle, User = user, Target = target, Move = move, Damage = dmg, IsCrit = isCrit };
            battle.RunModifyDamage(modEv);
            return Math.Max(1, modEv.Damage);
        }

        static bool IsType(Pokemon mon, MonType t)
        {
            if (mon?.Species == null) return false;
            return mon.Species.Type1 == t || mon.Species.Type2 == t;
        }
    }
}
