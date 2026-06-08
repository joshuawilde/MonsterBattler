namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Scrappy: Normal/Fighting moves can hit Ghost types. (logic in DamageCalc/Battle — this class is the type marker + registry entry.)</summary>
    public sealed class ScrappyEffect : Effect
    {
        public override string EffectId => "scrappy";
        public override string DisplayName => "Scrappy";
    }
}
