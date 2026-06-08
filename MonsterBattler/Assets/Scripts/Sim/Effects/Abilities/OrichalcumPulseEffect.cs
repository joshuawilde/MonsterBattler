using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Orichalcum Pulse: sets harsh sunlight when the owner switches in.</summary>
    public sealed class OrichalcumPulseEffect : Effect
    {
        public override string EffectId => "orichalcumpulse";
        public override string DisplayName => "Orichalcum Pulse";

        public override void OnSwitchIn(SwitchInEvent ev, Pokemon owner)
        {
            if (owner != ev.Pokemon) return;
            ev.Battle.SetWeather(Weather.Sun, 5);
        }
    }
}
