namespace MonsterBattler.Sim.Effects.Volatiles
{
    /// <summary>
    /// Marker volatile for two-turn charge moves. Battle.UseMove checks for its presence to
    /// know whether the user is mid-charge. No hooks — just identity.
    /// </summary>
    public sealed class TwoTurnChargeVolatile : Effect
    {
        public override string EffectId => "twoturncharge";
        public override string DisplayName => "Charging";
    }
}
