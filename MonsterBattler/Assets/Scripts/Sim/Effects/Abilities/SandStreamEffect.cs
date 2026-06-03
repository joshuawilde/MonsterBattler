using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class SandStreamEffect : Effect
    {
        public override string EffectId => "sandstream";
        public override string DisplayName => "Sand Stream";
        public override void OnSwitchIn(SwitchInEvent ev, Pokemon owner)
        {
            if (owner != ev.Pokemon) return;
            ev.Battle.SetWeather(Weather.Sandstorm, 5);
        }
    }
}
