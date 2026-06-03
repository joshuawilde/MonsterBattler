using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class LimberEffect : Effect
    {
        public override string EffectId => "limber";
        public override string DisplayName => "Limber";

        public override void OnTryStatus(TryStatusEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Status != StatusCondition.Paralysis) return;
            ev.Blocked = true;
            ev.BlockReason = "Limber";
        }
    }
}
