namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Magic Guard: prevents all damage except direct move hits. Status DoTs, Sandstorm chip,
    /// hazards, Life Orb recoil, recoil moves, Leech Seed, and Curse all do nothing to the owner.
    /// The blocking happens centrally in <see cref="Battle.ApplyDamage"/> via the DamageSource arg.
    /// </summary>
    public sealed class MagicGuardEffect : Effect
    {
        public override string EffectId => "magicguard";
        public override string DisplayName => "Magic Guard";
    }
}
