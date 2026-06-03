using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Volatiles
{
    /// <summary>
    /// Protect volatile: blocks any incoming move that respects the Protect flag (most damaging
    /// and most status moves). Lives one turn — the engine clears it at end of turn via the
    /// SingleTurn cleanup pass.
    /// </summary>
    public sealed class ProtectVolatile : Effect
    {
        public override string EffectId => "protect";
        public override string DisplayName => "Protect";

        public override void OnTryHit(TryHitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            if (ev.Move == null || !ev.Move.Protect) return;
            ev.Blocked = true;
            ev.BlockReason = "Protect";
        }
    }
}
