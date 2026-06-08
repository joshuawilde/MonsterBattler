using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>
    /// Curse (non-Ghost users): raises the user's Atk and Def by 1 and lowers its Spe by 1.
    /// The Ghost-type variant (HP cost + curse volatile) is not implemented.
    /// </summary>
    public sealed class CurseMoveEffect : Effect
    {
        public override string EffectId => "cursemove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var u = ev.User;
            if (u == null || u.IsFainted) return;

            ev.Battle.BoostStat(u, Stat.Atk, +1, u);
            ev.Battle.BoostStat(u, Stat.Def, +1, u);
            ev.Battle.BoostStat(u, Stat.Spe, -1, u);
        }
    }
}
