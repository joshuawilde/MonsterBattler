namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Teravolt: ignores the target's ability while attacking (logic in UseMove).</summary>
    public sealed class TeravoltEffect : Effect
    {
        public override string EffectId => "teravolt";
        public override string DisplayName => "Teravolt";
    }
}
