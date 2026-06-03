using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>
    /// Ember secondary effect: 10% chance to burn the target on a damaging hit.
    /// Invoked by the engine as a move-scoped effect (owner == null).
    /// </summary>
    public sealed class EmberEffect : Effect
    {
        public override string EffectId => "ember";
        public override string DisplayName => "Ember (secondary)";

        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (ev.Target == null || ev.Target.IsFainted) return;
            if (ev.Target.Status != StatusCondition.None) return;
            if (ev.Target.Species != null &&
                (ev.Target.Species.Type1 == MonType.Fire || ev.Target.Species.Type2 == MonType.Fire)) return;
            if (!ev.Battle.Prng.Chance(1, 10)) return;
            ev.Battle.ApplyStatus(ev.Target, StatusCondition.Burn);
        }
    }
}
