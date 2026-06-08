using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Anger Shell: when a hit drops the owner to half HP or below (crossing the threshold),
    /// raise Atk/SpA/Spe by 1 and lower Def/SpD by 1.
    /// </summary>
    public sealed class AngerShellEffect : Effect
    {
        public override string EffectId => "angershell";
        public override string DisplayName => "Anger Shell";

        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || owner.IsFainted) return;
            if (ev.DamageDealt <= 0) return;
            int maxHp = owner.MaxStats[(int)Stat.HP];
            int half = maxHp / 2;
            // Only trigger if this hit crossed from above-half to at-or-below-half.
            if (owner.CurrentHp > half) return;
            if (owner.CurrentHp + ev.DamageDealt <= half) return;
            ev.Battle.BoostStat(owner, Stat.Atk, +1);
            ev.Battle.BoostStat(owner, Stat.SpA, +1);
            ev.Battle.BoostStat(owner, Stat.Spe, +1);
            ev.Battle.BoostStat(owner, Stat.Def, -1);
            ev.Battle.BoostStat(owner, Stat.SpD, -1);
        }
    }
}
