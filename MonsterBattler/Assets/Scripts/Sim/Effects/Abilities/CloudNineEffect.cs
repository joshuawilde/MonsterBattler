namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Cloud Nine: weather effects are nullified while this Pokemon is active. Marker — Battle.ActiveWeather honors it.</summary>
    public sealed class CloudNineEffect : Effect
    {
        public override string EffectId => "cloudnine";
        public override string DisplayName => "Cloud Nine";
    }
}
