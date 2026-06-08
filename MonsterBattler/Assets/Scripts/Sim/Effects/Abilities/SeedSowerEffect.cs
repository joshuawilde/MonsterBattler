using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Seed Sower: sets Grassy Terrain whenever the owner is hit by an attack.</summary>
    public sealed class SeedSowerEffect : Effect
    {
        public override string EffectId => "seedsower";
        public override string DisplayName => "Seed Sower";

        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || owner.IsFainted) return;
            ev.Battle.SetTerrain(Terrain.Grassy, 5);
        }
    }
}
