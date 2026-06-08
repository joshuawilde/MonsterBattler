using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Rest: fully heals the user, curing any status and putting it to sleep.</summary>
    public sealed class RestMoveEffect : Effect
    {
        public override string EffectId => "restmove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var u = ev.User;
            if (u == null || u.IsFainted) return;
            int max = u.MaxStats[(int)Stat.HP];
            if (u.CurrentHp >= max) { ev.Battle.Log.Raw($"|-fail|{Name(u)}|heal"); return; }

            // Clear current status, then force sleep, then heal to full.
            u.Status = StatusCondition.None;
            u.StatusEffect = null;
            u.SleepTurnsLeft = 0;
            u.ToxicCounter = 0;
            ev.Battle.ApplyStatus(u, StatusCondition.Sleep); // may be blocked by sleep-immunity abilities
            u.CurrentHp = max;
            ev.Battle.Log.Raw($"|-heal|{Name(u)}|{u.CurrentHp}/{max}|[from] move: Rest");
        }

        static string Name(Pokemon m) => m?.Species?.Name ?? m?.Nickname ?? "?";
    }
}
