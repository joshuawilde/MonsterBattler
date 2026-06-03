using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class DrizzleEffect : Effect
    {
        public override string EffectId => "drizzle";
        public override string DisplayName => "Drizzle";
        public override void OnSwitchIn(SwitchInEvent ev, Pokemon owner)
        {
            if (owner != ev.Pokemon) return;
            ev.Battle.SetWeather(Weather.Rain, 5);
        }
    }
}
