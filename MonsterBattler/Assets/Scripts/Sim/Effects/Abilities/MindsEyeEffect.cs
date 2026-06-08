namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Mind's Eye: Normal/Fighting moves can hit Ghost types; ignores evasion. (logic in DamageCalc/Battle — this class is the type marker + registry entry.)</summary>
    public sealed class MindsEyeEffect : Effect
    {
        public override string EffectId => "mindseye";
        public override string DisplayName => "Mind's Eye";
    }
}
