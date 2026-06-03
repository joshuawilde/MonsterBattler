using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class StormDrainEffect : Effect
    {
        public override string EffectId => "stormdrain";
        public override string DisplayName => "Storm Drain";

        public override void OnTryHit(TryHitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move?.Type != MonType.Water) return;
            ev.Blocked = true;
            ev.BlockReason = "Storm Drain";
            ev.Battle.BoostStat(owner, Stat.SpA, +1);
        }
    }
}
