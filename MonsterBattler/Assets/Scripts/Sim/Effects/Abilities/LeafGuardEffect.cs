using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Leaf Guard: in harsh sunlight, the owner cannot be afflicted by status conditions.</summary>
    public sealed class LeafGuardEffect : Effect
    {
        public override string EffectId => "leafguard";
        public override string DisplayName => "Leaf Guard";

        public override void OnTryStatus(TryStatusEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            if (ev.Battle.Field.Weather != Weather.Sun) return;
            ev.Blocked = true;
            ev.BlockReason = "Leaf Guard";
        }
    }
}
