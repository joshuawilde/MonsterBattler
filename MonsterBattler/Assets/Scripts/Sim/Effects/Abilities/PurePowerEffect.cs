using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Pure Power: doubles Attack.</summary>
    public sealed class PurePowerEffect : Effect
    {
        public override string EffectId => "purepower";
        public override string DisplayName => "Pure Power";
        public override void OnModifyAtk(StatModifyEvent ev, Pokemon owner)
        {
            if (owner == ev.Owner) ev.Value *= 2;
        }
    }
}
