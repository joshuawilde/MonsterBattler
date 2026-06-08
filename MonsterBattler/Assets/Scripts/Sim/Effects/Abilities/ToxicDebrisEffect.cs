using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Toxic Debris: when the owner is hit by a physical move, a layer of Toxic Spikes is set
    /// on the attacker's side of the field (up to 2 layers).
    /// </summary>
    public sealed class ToxicDebrisEffect : Effect
    {
        public override string EffectId => "toxicdebris";
        public override string DisplayName => "Toxic Debris";

        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            if (ev.Move == null || ev.Move.Category != MoveCategory.Physical) return;
            if (ev.User == null || ev.User.IsFainted) return;
            var foeSide = ev.Battle.OpposingSideOf(owner);
            if (foeSide == null) return;
            ev.Battle.AddSideCondition(foeSide, "toxicspikes", maxLayers: 2);
        }
    }
}
