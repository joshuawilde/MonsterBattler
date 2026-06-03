using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.SideConditions
{
    /// <summary>Safeguard: blocks status conditions on the protected side for 5 turns.</summary>
    public sealed class SafeguardCondition : Effect
    {
        public override string EffectId => "safeguard";
        public override string DisplayName => "Safeguard";

        public override void OnTryStatus(TryStatusEvent ev, Pokemon owner)
        {
            // Dispatched via side conditions; owner is the would-be-statused mon.
            if (owner != ev.Target) return;
            ev.Blocked = true;
            ev.BlockReason = "Safeguard";
        }
    }
}
