namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Turboblaze: ignores the target's ability while attacking (logic in UseMove).</summary>
    public sealed class TurboblazeEffect : Effect
    {
        public override string EffectId => "turboblaze";
        public override string DisplayName => "Turboblaze";
    }
}
