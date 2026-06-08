namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Skill Link: multi-hit moves always hit max times (logic in UseMove).</summary>
    public sealed class SkillLinkEffect : Effect
    {
        public override string EffectId => "skilllink";
        public override string DisplayName => "Skill Link";
    }
}
