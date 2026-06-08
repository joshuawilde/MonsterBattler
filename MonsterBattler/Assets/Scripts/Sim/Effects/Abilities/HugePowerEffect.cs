using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Huge Power: doubles Attack.</summary>
    public sealed class HugePowerEffect : Effect
    {
        public override string EffectId => "hugepower";
        public override string DisplayName => "Huge Power";
        public override void OnModifyAtk(StatModifyEvent ev, Pokemon owner)
        {
            if (owner == ev.Owner) ev.Value *= 2;
        }
    }
}
