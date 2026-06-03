namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Clear Body: opponent-caused stat drops are refused. Marker class — block logic lives in Battle.BoostStat.</summary>
    public sealed class ClearBodyEffect : Effect
    {
        public override string EffectId => "clearbody";
        public override string DisplayName => "Clear Body";
    }
}
