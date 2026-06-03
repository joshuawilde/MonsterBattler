using System.Collections.Generic;
using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>
    /// Whirlwind / Roar / Dragon Tail: force target to switch out to a random non-fainted bench
    /// mon. Fails if target has no available swap. Bypasses accuracy (Whirlwind / Roar have
    /// accuracy 0 → always hits in our pipeline).
    /// </summary>
    public sealed class WhirlwindMoveEffect : Effect
    {
        public override string EffectId => "whirlwindmove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var t = ev.Target;
            if (t == null || t.IsFainted) return;
            var side = ev.Battle.SideOf(t);
            if (side == null) return;
            var alives = new List<int>();
            for (int i = 0; i < side.Team.Count; i++)
            {
                var p = side.Team[i];
                if (p == null || p == t || p.IsFainted) continue;
                alives.Add(i);
            }
            if (alives.Count == 0)
            {
                ev.Battle.Log.Raw($"|-fail|{t.Species?.Name ?? t.Nickname}");
                return;
            }
            int pick = alives[ev.Battle.Prng.Range(0, alives.Count)];
            // Bypass TrySwitch (which would refuse if trapped — Roar overrides trapping in PS).
            ev.Battle.Switch(side, pick);
        }
    }
}
