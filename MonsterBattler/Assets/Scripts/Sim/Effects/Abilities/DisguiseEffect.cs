namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Disguise: absorbs the first hit and breaks (logic in UseMove hit loop).</summary>
    public sealed class DisguiseEffect : Effect
    {
        public override string EffectId => "disguise";
        public override string DisplayName => "Disguise";
    }
}
