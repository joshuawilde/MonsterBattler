using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class ElectricSurgeEffect : Effect
    {
        public override string EffectId => "electricsurge";
        public override string DisplayName => "ElectricSurge";
        public override void OnSwitchIn(SwitchInEvent ev, Pokemon owner)
        {
            if (owner != ev.Pokemon) return;
            ev.Battle.SetTerrain(Terrain.Electric, 5);
        }
    }
}
