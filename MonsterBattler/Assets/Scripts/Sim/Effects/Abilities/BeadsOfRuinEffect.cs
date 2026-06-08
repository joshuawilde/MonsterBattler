namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Beads of Ruin: lowers the Sp. Def of all other Pokémon by 25%. (logic in DamageCalc/Battle — this class is the type marker + registry entry.)</summary>
    public sealed class BeadsOfRuinEffect : Effect
    {
        public override string EffectId => "beadsofruin";
        public override string DisplayName => "Beads of Ruin";
    }
}
