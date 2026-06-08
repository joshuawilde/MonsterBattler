using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Comatose: the owner is treated as permanently drowsing and cannot be afflicted by any
    /// major status condition. Modeled here as full status immunity.
    /// </summary>
    public sealed class ComatoseEffect : Effect
    {
        public override string EffectId => "comatose";
        public override string DisplayName => "Comatose";

        public override void OnTryStatus(TryStatusEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            if (ev.Status == StatusCondition.None) return;
            ev.Blocked = true;
            ev.BlockReason = "Comatose";
        }
    }
}
