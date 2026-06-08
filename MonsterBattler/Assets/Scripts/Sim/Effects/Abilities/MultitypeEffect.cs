using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Multitype: Arceus takes the type of its held Plate (RKS Memory handled the same way).</summary>
    public sealed class MultitypeEffect : Effect
    {
        public override string EffectId => "multitype";
        public override string DisplayName => "Multitype";
        public override void OnSwitchIn(SwitchInEvent ev, Pokemon owner)
        {
            if (owner != ev.Pokemon || owner.Item == null) return;
            var t = PlateType(owner.Item.Id);
            if (t != MonType.None) { owner.TypeOverridden = true; owner.OType1 = t; owner.OType2 = MonType.None; }
        }
        static MonType PlateType(string item) => item switch
        {
            "flameplate" => MonType.Fire, "splashplate" => MonType.Water, "zapplate" => MonType.Electric,
            "meadowplate" => MonType.Grass, "icicleplate" => MonType.Ice, "fistplate" => MonType.Fighting,
            "toxicplate" => MonType.Poison, "earthplate" => MonType.Ground, "skyplate" => MonType.Flying,
            "mindplate" => MonType.Psychic, "insectplate" => MonType.Bug, "stoneplate" => MonType.Rock,
            "spookyplate" => MonType.Ghost, "dracoplate" => MonType.Dragon, "dreadplate" => MonType.Dark,
            "ironplate" => MonType.Steel, "pixieplate" => MonType.Fairy, _ => MonType.None,
        };
    }
}
