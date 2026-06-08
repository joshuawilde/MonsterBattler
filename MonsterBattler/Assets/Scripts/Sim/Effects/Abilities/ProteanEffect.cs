namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Protean: changes the user's type to its move's type each switch-in (logic in UseMove).</summary>
    public sealed class ProteanEffect : Effect
    {
        public override string EffectId => "protean";
        public override string DisplayName => "Protean";
    }
}
