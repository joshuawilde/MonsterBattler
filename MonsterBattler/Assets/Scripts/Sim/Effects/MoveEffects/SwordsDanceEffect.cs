using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    public sealed class SwordsDanceEffect : Effect
    {
        public override string EffectId => "swordsdance";
        public override void OnHit(HitEvent ev, Pokemon owner)
            => ev.Battle.BoostStat(ev.User, Stat.Atk, +2);
    }
}
