namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Overcoat: immune to powder moves and weather chip damage (logic in UseMove / TickWeather).</summary>
    public sealed class OvercoatEffect : Effect
    {
        public override string EffectId => "overcoat";
        public override string DisplayName => "Overcoat";
    }
}
