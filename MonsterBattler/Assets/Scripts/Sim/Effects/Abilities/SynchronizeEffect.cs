namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Synchronize: reflects burn/poison/paralysis back onto the attacker (logic in Battle.ApplyStatus).</summary>
    public sealed class SynchronizeEffect : Effect
    {
        public override string EffectId => "synchronize";
        public override string DisplayName => "Synchronize";
    }
}
