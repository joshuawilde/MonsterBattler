namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Contrary: inverts the owner's stat changes (logic in Battle.BoostStat).</summary>
    public sealed class ContraryEffect : Effect
    {
        public override string EffectId => "contrary";
        public override string DisplayName => "Contrary";
    }
}
