namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Power Spot: boosts allies' moves (no effect in singles).</summary>
    public sealed class PowerspotEffect : Effect
    {
        public override string EffectId => "powerspot";
        public override string DisplayName => "Power Spot";
    }
}
