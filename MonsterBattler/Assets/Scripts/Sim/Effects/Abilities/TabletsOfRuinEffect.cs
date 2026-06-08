namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Tablets of Ruin: lowers the Attack of all other Pokémon by 25%. (logic in DamageCalc/Battle — this class is the type marker + registry entry.)</summary>
    public sealed class TabletsOfRuinEffect : Effect
    {
        public override string EffectId => "tabletsofruin";
        public override string DisplayName => "Tablets of Ruin";
    }
}
