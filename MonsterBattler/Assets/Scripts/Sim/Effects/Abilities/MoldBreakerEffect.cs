namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Mold Breaker: ignores the target's ability while attacking (logic in UseMove).</summary>
    public sealed class MoldBreakerEffect : Effect
    {
        public override string EffectId => "moldbreaker";
        public override string DisplayName => "Mold Breaker";
    }
}
