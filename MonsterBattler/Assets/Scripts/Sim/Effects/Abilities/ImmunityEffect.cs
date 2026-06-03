using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class ImmunityEffect : Effect
    {
        public override string EffectId => "immunity";
        public override string DisplayName => "Immunity";

        public override void OnTryStatus(TryStatusEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            if (ev.Status != StatusCondition.Poison && ev.Status != StatusCondition.BadlyPoisoned) return;
            ev.Blocked = true;
            ev.BlockReason = "Immunity";
        }
    }
}
