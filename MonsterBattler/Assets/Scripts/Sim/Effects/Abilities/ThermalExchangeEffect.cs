using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Thermal Exchange: raises the owner's Attack by 1 stage when hit by a Fire-type move,
    /// and prevents the owner from being burned.
    /// </summary>
    public sealed class ThermalExchangeEffect : Effect
    {
        public override string EffectId => "thermalexchange";
        public override string DisplayName => "Thermal Exchange";

        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            if (ev.Move?.Type != MonType.Fire) return;
            ev.Battle.BoostStat(owner, Stat.Atk, 1, source: owner);
        }

        public override void OnTryStatus(TryStatusEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Status != StatusCondition.Burn) return;
            ev.Blocked = true;
            ev.BlockReason = "Thermal Exchange";
        }
    }
}
