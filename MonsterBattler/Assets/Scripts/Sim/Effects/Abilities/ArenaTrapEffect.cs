namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Arena Trap: prevents grounded foes from switching out (logic in Battle.IsTrapped).</summary>
    public sealed class ArenaTrapEffect : Effect
    {
        public override string EffectId => "arenatrap";
        public override string DisplayName => "Arena Trap";
    }
}
