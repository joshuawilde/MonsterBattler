using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    public sealed class CalmMindEffect : Effect
    {
        public override string EffectId => "calmmind";
        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            ev.Battle.BoostStat(ev.User, Stat.SpA, +1);
            ev.Battle.BoostStat(ev.User, Stat.SpD, +1);
        }
    }
}
