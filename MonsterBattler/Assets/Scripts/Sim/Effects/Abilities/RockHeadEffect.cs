namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Rock Head: no recoil damage. (logic in DamageCalc/Battle — this class is the type marker + registry entry.)</summary>
    public sealed class RockHeadEffect : Effect
    {
        public override string EffectId => "rockhead";
        public override string DisplayName => "Rock Head";
    }
}
