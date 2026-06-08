using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class VitalSpiritEffect : Effect
    {
        public override string EffectId => "vitalspirit";
        public override string DisplayName => "Vital Spirit";
        public override void OnTryStatus(TryStatusEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            if (ev.Status != StatusCondition.Sleep) return;
            ev.Blocked = true; ev.BlockReason = "Vital Spirit";
        }
    }
}
