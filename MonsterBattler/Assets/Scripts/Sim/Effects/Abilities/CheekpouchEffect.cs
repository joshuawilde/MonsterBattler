namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Cheek Pouch: heals 1/3 max HP whenever the holder eats a Berry (logic in Battle.ConsumeItem).</summary>
    public sealed class CheekPouchEffect : Effect
    {
        public override string EffectId => "cheekpouch";
        public override string DisplayName => "Cheek Pouch";
    }
}
