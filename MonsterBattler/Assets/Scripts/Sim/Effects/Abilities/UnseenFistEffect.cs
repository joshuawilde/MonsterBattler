namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Unseen Fist: the owner's contact moves bypass Protect (logic in ProtectVolatile).</summary>
    public sealed class UnseenFistEffect : Effect
    {
        public override string EffectId => "unseenfist";
        public override string DisplayName => "UnseenFist";
    }
}
