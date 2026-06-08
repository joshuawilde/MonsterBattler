namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Frisk: reveals the foe's held item — purely informational, mechanically inert.</summary>
    public sealed class FriskEffect : Effect
    {
        public override string EffectId => "frisk";
        public override string DisplayName => "Frisk";
    }
}
