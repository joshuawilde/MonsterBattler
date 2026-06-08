using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Berserk: when the owner is damaged by a move and its HP drops to ≤ 1/2 (crossing the
    /// half-HP threshold from above), raise its Special Attack by 1 stage.
    /// </summary>
    public sealed class BerserkEffect : Effect
    {
        public override string EffectId => "berserk";
        public override string DisplayName => "Berserk";

        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            if (ev.DamageDealt <= 0) return;
            if (owner.IsFainted) return;

            int maxHp = owner.MaxStats[(int)Stat.HP];
            int hpAfter = owner.CurrentHp;
            int hpBefore = hpAfter + ev.DamageDealt;

            // Must have been above half before, and at/below half after.
            if (hpBefore * 2 <= maxHp) return;
            if (hpAfter * 2 > maxHp) return;

            ev.Battle.BoostStat(owner, Stat.SpA, 1, source: owner);
        }
    }
}
