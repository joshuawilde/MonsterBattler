namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Unnerve: would stop foes eating Berries — the Berry item system is not modeled, so this is inert.</summary>
    public sealed class UnnerveEffect : Effect
    {
        public override string EffectId => "unnerve";
        public override string DisplayName => "Unnerve";
    }
}
