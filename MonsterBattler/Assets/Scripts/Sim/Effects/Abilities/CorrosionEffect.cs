namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Corrosion: can poison Steel- and Poison-type targets (logic in Battle.ApplyStatus).</summary>
    public sealed class CorrosionEffect : Effect
    {
        public override string EffectId => "corrosion";
        public override string DisplayName => "Corrosion";
    }
}
