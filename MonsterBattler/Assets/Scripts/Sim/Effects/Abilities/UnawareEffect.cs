namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Unaware: ignores the opponent's stat-stage changes when calculating damage. (logic in DamageCalc/Battle — this class is the type marker + registry entry.)</summary>
    public sealed class UnawareEffect : Effect
    {
        public override string EffectId => "unaware";
        public override string DisplayName => "Unaware";
    }
}
