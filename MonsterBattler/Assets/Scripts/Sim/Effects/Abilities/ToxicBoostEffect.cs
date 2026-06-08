using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Toxic Boost: while the owner is poisoned, its physical Attack is multiplied by 1.5×.
    /// </summary>
    public sealed class ToxicBoostEffect : Effect
    {
        public override string EffectId => "toxicboost";
        public override string DisplayName => "Toxic Boost";

        public override void OnModifyAtk(StatModifyEvent ev, Pokemon owner)
        {
            if (owner != ev.Owner) return;
            if (owner.Status != StatusCondition.Poison && owner.Status != StatusCondition.BadlyPoisoned) return;
            ev.Value = ev.Value * 3 / 2;
        }
    }
}
