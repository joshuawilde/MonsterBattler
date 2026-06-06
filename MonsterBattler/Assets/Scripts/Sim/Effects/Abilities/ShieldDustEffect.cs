namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Shield Dust: blocks the target-facing part of incoming chance-based secondaries. Applied by
    /// reference in <see cref="Battle.ApplySecondaries"/> (no event hooks of its own).
    /// </summary>
    public sealed class ShieldDustEffect : Effect
    {
        public override string EffectId => "shielddust";
        public override string DisplayName => "Shield Dust";
    }
}
