using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    public sealed class NastyPlotEffect : Effect
    {
        public override string EffectId => "nastyplot";
        public override void OnHit(HitEvent ev, Pokemon owner)
            => ev.Battle.BoostStat(ev.User, Stat.SpA, +2);
    }
}
