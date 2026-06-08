using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class GrassySurgeEffect : Effect
    {
        public override string EffectId => "grassysurge";
        public override string DisplayName => "GrassySurge";
        public override void OnSwitchIn(SwitchInEvent ev, Pokemon owner)
        {
            if (owner != ev.Pokemon) return;
            ev.Battle.SetTerrain(Terrain.Grassy, 5);
        }
    }
}
