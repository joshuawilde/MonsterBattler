namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Light Metal: changes the holder's battle weight (logic in Battle.EffectiveWeight).</summary>
    public sealed class LightmetalEffect : Effect
    {
        public override string EffectId => "lightmetal";
        public override string DisplayName => "Light Metal";
    }
}
