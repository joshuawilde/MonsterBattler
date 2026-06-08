using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class PsychicSurgeEffect : Effect
    {
        public override string EffectId => "psychicsurge";
        public override string DisplayName => "PsychicSurge";
        public override void OnSwitchIn(SwitchInEvent ev, Pokemon owner)
        {
            if (owner != ev.Pokemon) return;
            ev.Battle.SetTerrain(Terrain.Psychic, 5);
        }
    }
}
