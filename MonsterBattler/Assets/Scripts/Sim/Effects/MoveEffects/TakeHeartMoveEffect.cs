using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Take Heart: raises user's SpA and SpD by 1 and cures the user's major status.</summary>
    public sealed class TakeHeartMoveEffect : Effect
    {
        public override string EffectId => "takeheartmove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var u = ev.User;
            if (u == null || u.IsFainted) return;

            ev.Battle.BoostStat(u, Stat.SpA, +1, u);
            ev.Battle.BoostStat(u, Stat.SpD, +1, u);

            if (u.Status != StatusCondition.None)
            {
                u.Status = StatusCondition.None;
                u.StatusEffect = null;
                u.SleepTurnsLeft = 0;
                u.ToxicCounter = 0;
                ev.Battle.Log.Raw($"|-curestatus|{Name(u)}|[from] move: Take Heart");
            }
        }

        static string Name(Pokemon m) => m?.Species?.Name ?? m?.Nickname ?? "?";
    }
}
