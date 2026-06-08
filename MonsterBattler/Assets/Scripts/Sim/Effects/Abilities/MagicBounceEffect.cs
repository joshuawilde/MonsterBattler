namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Magic Bounce: reflects an opposing status move back at its user (logic in UseMove).</summary>
    public sealed class MagicBounceEffect : Effect
    {
        public override string EffectId => "magicbounce";
        public override string DisplayName => "Magic Bounce";
    }
}
