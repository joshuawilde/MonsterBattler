namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Heavy Metal: changes the holder's battle weight (logic in Battle.EffectiveWeight).</summary>
    public sealed class HeavymetalEffect : Effect
    {
        public override string EffectId => "heavymetal";
        public override string DisplayName => "Heavy Metal";
    }
}
