using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class DroughtEffect : Effect
    {
        public override string EffectId => "drought";
        public override string DisplayName => "Drought";
        public override void OnSwitchIn(SwitchInEvent ev, Pokemon owner)
        {
            if (owner != ev.Pokemon) return;
            ev.Battle.SetWeather(Weather.Sun, 5);
        }
    }
}
