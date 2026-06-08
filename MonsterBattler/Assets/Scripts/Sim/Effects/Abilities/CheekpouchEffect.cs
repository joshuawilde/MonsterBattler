namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Cheek Pouch: would heal on eating a Berry — the Berry item system is not modeled, so this is inert.</summary>
    public sealed class CheekpouchEffect : Effect
    {
        public override string EffectId => "cheekpouch";
        public override string DisplayName => "Cheekpouch";
    }
}
