namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Magnet Pull: prevents Steel-type foes from switching out (logic in Battle.IsTrapped).</summary>
    public sealed class MagnetPullEffect : Effect
    {
        public override string EffectId => "magnetpull";
        public override string DisplayName => "Magnet Pull";
    }
}
