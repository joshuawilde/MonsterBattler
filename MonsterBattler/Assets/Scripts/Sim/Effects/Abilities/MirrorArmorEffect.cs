namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Mirror Armor: reflects stat drops back at the attacker (logic in BoostStat).</summary>
    public sealed class MirrorArmorEffect : Effect
    {
        public override string EffectId => "mirrorarmor";
        public override string DisplayName => "Mirror Armor";
    }
}
