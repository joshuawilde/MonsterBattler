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

            // ModifyType lets -ate abilities rewrite Normal moves to a different type + add BP%.
            var typeEv = new ModifyTypeEvent { Battle = battle, User = user, Move = move, Type = move.Type, BasePowerBonus = 0 };
            battle.RunModifyType(typeEv);
            MonType effectiveType = typeEv.Type;
            int typeBonusPct = typeEv.BasePowerBonus;

            var bpEv = new BasePowerEvent { Battle = battle, User = user, Target = target, Move = move, BasePower = move.BasePower };
            battle.RunBasePower(bpEv);
            // The move's own effect also gets to tweak base power (Knock Off ×1.5 on item, etc.).
            var moveEffect = Effects.EffectRegistry.Get(move.EffectId);
            if (moveEffect != null) moveEffect.OnBasePower(bpEv, null);
            int basePower = Math.Max(1, bpEv.BasePower);
            if (typeBonusPct > 0) basePower = basePower * (100 + typeBonusPct) / 100;

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
            // Unaware: the attacker ignores the defender's defensive stages; the defender ignores the attacker's offensive stages.
            if (user.AbilityEffect is Effects.Abilities.UnawareEffect) defStage = 0;
            if (target.AbilityEffect is Effects.Abilities.UnawareEffect) atkStage = 0;
            int atkStat = Math.Max(1, (int)(user.MaxStats[(int)atkStatKind] * Stats.StageMult(atkStage)));
            int defStat = Math.Max(1, (int)(target.MaxStats[(int)defStatKind] * Stats.StageMult(defStage)));

            var atkEv = new StatModifyEvent { Battle = battle, Owner = user, Stat = atkStatKind, Value = atkStat, ContextMove = move };
            if (atkStatKind == Stat.Atk) battle.RunModifyAtk(atkEv); else battle.RunModifySpA(atkEv);
            atkStat = Math.Max(1, atkEv.Value);

            var defEv = new StatModifyEvent { Battle = battle, Owner = target, Stat = defStatKind, Value = defStat, ContextMove = move };
            if (defStatKind == Stat.Def) battle.RunModifyDef(defEv); else battle.RunModifySpD(defEv);
            defStat = Math.Max(1, defEv.Value);

            // Ruin abilities: a holder lowers the matching stat of ALL other active mons by 25%.
            if (atkStatKind == Stat.Atk && OtherActiveHas<Effects.Abilities.TabletsOfRuinEffect>(battle, user)) atkStat = atkStat * 3 / 4;
            if (atkStatKind == Stat.SpA && OtherActiveHas<Effects.Abilities.VesselOfRuinEffect>(battle, user)) atkStat = atkStat * 3 / 4;
            if (defStatKind == Stat.Def && OtherActiveHas<Effects.Abilities.SwordOfRuinEffect>(battle, target)) defStat = defStat * 3 / 4;
            if (defStatKind == Stat.SpD && OtherActiveHas<Effects.Abilities.BeadsOfRuinEffect>(battle, target)) defStat = defStat * 3 / 4;
            atkStat = Math.Max(1, atkStat); defStat = Math.Max(1, defStat);

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
            var (uType1, uType2) = user.CurrentTypes();
            bool originalStab = effectiveType != MonType.None && (effectiveType == uType1 || effectiveType == uType2);
            bool teraStab = user.IsTerastallized && effectiveType == user.TeraType;
            if (originalStab && teraStab) dmg = dmg * 2;
            else if (originalStab || teraStab) dmg = dmg * 3 / 2;

            // Defensive types (Tera / Protean override / species).
            var (defType1, defType2) = target.CurrentTypes();
            float eff = TypeChart.Effectiveness(effectiveType, defType1, defType2);
            // Scrappy / Mind's Eye: Normal & Fighting moves ignore the Ghost-type immunity.
            if (eff == 0f && (effectiveType == MonType.Normal || effectiveType == MonType.Fighting) &&
                (defType1 == MonType.Ghost || defType2 == MonType.Ghost) &&
                (user.AbilityEffect is Effects.Abilities.ScrappyEffect || user.AbilityEffect is Effects.Abilities.MindsEyeEffect))
            {
                MonType d1 = defType1 == MonType.Ghost ? MonType.None : defType1;
                MonType d2 = defType2 == MonType.Ghost ? MonType.None : defType2;
                eff = TypeChart.Effectiveness(effectiveType, d1, d2);
            }
            dmg = (int)(dmg * eff);

            // Weather damage multipliers (after STAB/type, before crit). Cloud Nine / Air Lock
            // suppresses weather effects globally via ActiveWeather().
            switch (battle.ActiveWeather())
            {
                case Weather.Sun:
                    if (effectiveType == MonType.Fire) dmg = dmg * 3 / 2;
                    else if (effectiveType == MonType.Water) dmg = dmg / 2;
                    break;
                case Weather.Rain:
                    if (effectiveType == MonType.Water) dmg = dmg * 3 / 2;
                    else if (effectiveType == MonType.Fire) dmg = dmg / 2;
                    break;
                case Weather.HarshSun:
                    if (effectiveType == MonType.Water) return 0; // Water moves fail in extremely harsh sunlight.
                    if (effectiveType == MonType.Fire) dmg = dmg * 3 / 2;
                    break;
                case Weather.HeavyRain:
                    if (effectiveType == MonType.Fire) return 0;
                    if (effectiveType == MonType.Water) dmg = dmg * 3 / 2;
                    break;
            }

            if (isCrit)
            {
                dmg = dmg * 3 / 2;
                if (user.AbilityEffect is Effects.Abilities.SniperEffect) dmg = dmg * 3 / 2; // 1.5×1.5 = 2.25
            }

            var modEv = new ModifyDamageEvent { Battle = battle, User = user, Target = target, Move = move, Damage = dmg, IsCrit = isCrit };
            battle.RunModifyDamage(modEv);
            return Math.Max(1, modEv.Damage);
        }

        static bool IsType(Pokemon mon, MonType t)
        {
            if (mon?.Species == null) return false;
            return mon.Species.Type1 == t || mon.Species.Type2 == t;
        }

        // True if any active mon other than `exclude` has ability effect T (for the Ruin auras).
        static bool OtherActiveHas<T>(Battle b, Pokemon exclude) where T : Effects.Effect
        {
            foreach (var side in b.Sides)
                foreach (var m in side.ActiveSlots)
                    if (m != null && m != exclude && !m.IsFainted && m.AbilityEffect is T) return true;
            return false;
        }
    }
}
