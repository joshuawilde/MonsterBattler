using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class SapSipperEffect : Effect
    {
        public override string EffectId => "sapsipper";
        public override string DisplayName => "Sap Sipper";

        public override void OnTryHit(TryHitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move?.Type != MonType.Grass) return;
            ev.Blocked = true;
            ev.BlockReason = "Sap Sipper";
            ev.Battle.BoostStat(owner, Stat.Atk, +1);
        }
    }
}
