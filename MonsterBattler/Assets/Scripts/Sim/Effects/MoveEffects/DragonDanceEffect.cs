using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    public sealed class DragonDanceEffect : Effect
    {
        public override string EffectId => "dragondance";
        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            ev.Battle.BoostStat(ev.User, Stat.Atk, +1);
            ev.Battle.BoostStat(ev.User, Stat.Spe, +1);
        }
    }
}
