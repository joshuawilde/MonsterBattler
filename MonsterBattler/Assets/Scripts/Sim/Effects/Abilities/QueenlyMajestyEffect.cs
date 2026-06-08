namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Queenly Majesty: blocks opposing priority moves (logic in UseMove).</summary>
    public sealed class QueenlyMajestyEffect : Effect
    {
        public override string EffectId => "queenlymajesty";
        public override string DisplayName => "Queenly Majesty";
    }
}
