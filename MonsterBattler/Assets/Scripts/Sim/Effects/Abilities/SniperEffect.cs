namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Sniper: critical hits deal 2.25× instead of 1.5×. (logic in DamageCalc/Battle — this class is the type marker + registry entry.)</summary>
    public sealed class SniperEffect : Effect
    {
        public override string EffectId => "sniper";
        public override string DisplayName => "Sniper";
    }
}
