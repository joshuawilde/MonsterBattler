namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Sticky Hold: the held item can't be stolen (checked in Magician / Pickpocket).</summary>
    public sealed class StickyHoldEffect : Effect
    {
        public override string EffectId => "stickyhold";
        public override string DisplayName => "Sticky Hold";
    }
}
