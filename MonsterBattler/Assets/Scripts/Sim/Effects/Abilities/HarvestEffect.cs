namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Harvest: would restore a consumed Berry — the Berry item system is not modeled, so this is inert.</summary>
    public sealed class HarvestEffect : Effect
    {
        public override string EffectId => "harvest";
        public override string DisplayName => "Harvest";
    }
}
