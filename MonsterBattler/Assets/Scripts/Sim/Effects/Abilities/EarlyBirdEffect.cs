namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Early Bird: wakes from sleep twice as fast (logic in SleepStatus).</summary>
    public sealed class EarlyBirdEffect : Effect
    {
        public override string EffectId => "earlybird";
        public override string DisplayName => "Early Bird";
    }
}
