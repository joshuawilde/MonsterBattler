namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Liquid Ooze: drain moves damage the drainer (logic in UseMove).</summary>
    public sealed class LiquidOozeEffect : Effect
    {
        public override string EffectId => "liquidooze";
        public override string DisplayName => "Liquid Ooze";
    }
}
