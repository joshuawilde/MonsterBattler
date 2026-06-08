namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Dancer: immediately copies any dance move used by another Pokemon (logic in UseMove).</summary>
    public sealed class DancerEffect : Effect
    {
        public override string EffectId => "dancer";
        public override string DisplayName => "Dancer";
    }
}
