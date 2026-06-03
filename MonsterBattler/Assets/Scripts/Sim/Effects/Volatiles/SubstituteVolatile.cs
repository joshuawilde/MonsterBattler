using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Volatiles
{
    /// <summary>
    /// Substitute volatile: <see cref="VolatileSlot.Counter"/> stores the substitute's HP. Damage
    /// routing is handled in <see cref="Battle.UseMove"/> directly so the substitute fully
    /// absorbs the hit (gen 5+ semantics — no bleed-through when broken).
    /// This effect blocks most status moves at <see cref="OnTryHit"/>; sound-flagged moves bypass.
    /// </summary>
    public sealed class SubstituteVolatile : Effect
    {
        public override string EffectId => "substitute";
        public override string DisplayName => "Substitute";

        public override void OnTryHit(TryHitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move == null) return;
            if (ev.Move.Category != MoveCategory.Status) return;     // damage is routed in UseMove
            if (ev.Move.Sound) return;                                // sound moves bypass
            if (ev.User == owner) return;                             // self-targeting status passes through
            ev.Blocked = true;
            ev.BlockReason = "Substitute";
        }
    }
}
