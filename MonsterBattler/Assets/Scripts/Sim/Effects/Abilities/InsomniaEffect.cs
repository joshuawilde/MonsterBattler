using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class InsomniaEffect : Effect
    {
        public override string EffectId => "insomnia";
        public override string DisplayName => "Insomnia";

        public override void OnTryStatus(TryStatusEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Status != StatusCondition.Sleep) return;
            ev.Blocked = true;
            ev.BlockReason = "Insomnia";
        }
    }
}
