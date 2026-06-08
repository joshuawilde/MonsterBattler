using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Toxic Chain: 30% chance to badly poison the target when the owner lands a damaging move.
    /// </summary>
    public sealed class ToxicChainEffect : Effect
    {
        public override string EffectId => "toxicchain";
        public override string DisplayName => "Toxic Chain";

        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.User) return;
            if (ev.Target == null || ev.Target.IsFainted) return;
            if (ev.Target.Status != StatusCondition.None) return;
            if (!ev.Battle.Prng.Chance(3, 10)) return;
            ev.Battle.ApplyStatus(ev.Target, StatusCondition.BadlyPoisoned);
        }
    }
}
