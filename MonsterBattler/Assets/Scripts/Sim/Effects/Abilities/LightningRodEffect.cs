using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class LightningRodEffect : Effect
    {
        public override string EffectId => "lightningrod";
        public override string DisplayName => "Lightning Rod";

        public override void OnTryHit(TryHitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move?.Type != MonType.Electric) return;
            ev.Blocked = true;
            ev.BlockReason = "Lightning Rod";
            ev.Battle.BoostStat(owner, Stat.SpA, +1);
        }
    }
}
