namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Ice Face: absorbs the first physical hit and breaks (logic in UseMove hit loop).</summary>
    public sealed class IceFaceEffect : Effect
    {
        public override string EffectId => "iceface";
        public override string DisplayName => "Ice Face";
    }
}
