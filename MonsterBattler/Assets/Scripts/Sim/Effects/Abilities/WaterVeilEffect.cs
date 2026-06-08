using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class WaterVeilEffect : Effect
    {
        public override string EffectId => "waterveil";
        public override string DisplayName => "Water Veil";
        public override void OnTryStatus(TryStatusEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            if (ev.Status != StatusCondition.Burn) return;
            ev.Blocked = true; ev.BlockReason = "Water Veil";
        }
    }
}
