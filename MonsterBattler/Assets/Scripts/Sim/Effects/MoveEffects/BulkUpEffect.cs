using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    public sealed class BulkUpEffect : Effect
    {
        public override string EffectId => "bulkup";
        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            ev.Battle.BoostStat(ev.User, Stat.Atk, +1);
            ev.Battle.BoostStat(ev.User, Stat.Def, +1);
        }
    }
}
