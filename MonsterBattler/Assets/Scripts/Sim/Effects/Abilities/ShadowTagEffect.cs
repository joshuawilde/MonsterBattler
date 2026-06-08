namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Shadow Tag: prevents foes from switching out (logic in Battle.IsTrapped).</summary>
    public sealed class ShadowTagEffect : Effect
    {
        public override string EffectId => "shadowtag";
        public override string DisplayName => "Shadow Tag";
    }
}
