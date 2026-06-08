using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Volatiles
{
    /// <summary>Magnet Rise: owner is immune to Ground-type moves for ~5 turns.</summary>
    public sealed class MagnetRiseVolatile : Effect
    {
        public override string EffectId => "magnetrise";
        public override string DisplayName => "Magnet Rise";

        public override void OnTryHit(TryHitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            if (ev.Move?.Type == MonType.Ground)
            {
                ev.Blocked = true;
                ev.BlockReason = "Magnet Rise";
            }
        }

        public override void OnResidual(ResidualEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            var slot = owner.GetVolatile("magnetrise");
            if (slot == null) return;
            if (slot.Turns <= 1) { ev.Battle.RemoveVolatile(owner, "magnetrise"); return; }
            slot.Turns--;
        }
    }
}
