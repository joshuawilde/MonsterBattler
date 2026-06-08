namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Shell Armor: cannot be struck by a critical hit. (logic in DamageCalc/Battle — this class is the type marker + registry entry.)</summary>
    public sealed class ShellArmorEffect : Effect
    {
        public override string EffectId => "shellarmor";
        public override string DisplayName => "Shell Armor";
    }
}
