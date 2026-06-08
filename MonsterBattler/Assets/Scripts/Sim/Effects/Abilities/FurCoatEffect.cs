using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Fur Coat: doubles Defense.</summary>
    public sealed class FurCoatEffect : Effect
    {
        public override string EffectId => "furcoat";
        public override string DisplayName => "Fur Coat";
        public override void OnModifyDef(StatModifyEvent ev, Pokemon owner)
        {
            if (owner == ev.Owner) ev.Value *= 2;
        }
    }
}
