namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Keen Eye: accuracy cannot be lowered (logic in BoostStat).</summary>
    public sealed class KeenEyeEffect : Effect
    {
        public override string EffectId => "keeneye";
        public override string DisplayName => "Keen Eye";
    }
}
