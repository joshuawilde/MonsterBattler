namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Sword of Ruin: lowers the Defense of all other Pokémon by 25%. (logic in DamageCalc/Battle — this class is the type marker + registry entry.)</summary>
    public sealed class SwordOfRuinEffect : Effect
    {
        public override string EffectId => "swordofruin";
        public override string DisplayName => "Sword of Ruin";
    }
}
