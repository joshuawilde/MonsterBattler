namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Damp: prevents explosion moves (logic in UseMove).</summary>
    public sealed class DampEffect : Effect
    {
        public override string EffectId => "damp";
        public override string DisplayName => "Damp";
    }
}
