namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Pressure: attacking moves cost an extra PP (logic in UseMove).</summary>
    public sealed class PressureEffect : Effect
    {
        public override string EffectId => "pressure";
        public override string DisplayName => "Pressure";
    }
}
