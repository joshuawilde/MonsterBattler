namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Serene Grace: doubles the chance of a move's secondary effects. Applied by reference in
    /// <see cref="Battle.ApplySecondaries"/> (no event hooks of its own).
    /// </summary>
    public sealed class SereneGraceEffect : Effect
    {
        public override string EffectId => "serenegrace";
        public override string DisplayName => "Serene Grace";
    }
}
