using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class SnowWarningEffect : Effect
    {
        public override string EffectId => "snowwarning";
        public override string DisplayName => "Snow Warning";
        public override void OnSwitchIn(SwitchInEvent ev, Pokemon owner)
        {
            if (owner != ev.Pokemon) return;
            ev.Battle.SetWeather(Weather.Snow, 5);
        }
    }
}
