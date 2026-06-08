namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Vessel of Ruin: lowers the Sp. Atk of all other Pokémon by 25%. (logic in DamageCalc/Battle — this class is the type marker + registry entry.)</summary>
    public sealed class VesselOfRuinEffect : Effect
    {
        public override string EffectId => "vesselofruin";
        public override string DisplayName => "Vessel of Ruin";
    }
}
