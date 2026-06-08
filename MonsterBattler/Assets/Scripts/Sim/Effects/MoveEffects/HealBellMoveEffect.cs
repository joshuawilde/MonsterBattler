using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Heal Bell / Aromatherapy: cures the major status of the user's whole team.</summary>
    public sealed class HealBellMoveEffect : Effect
    {
        public override string EffectId => "healbellmove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var u = ev.User;
            if (u == null) return;
            foreach (var side in ev.Battle.Sides)
            {
                if (!side.Team.Contains(u)) continue;
                foreach (var m in side.Team)
                {
                    if (m == null || m.Status == StatusCondition.None) continue;
                    m.Status = StatusCondition.None;
                    m.StatusEffect = null;
                    m.SleepTurnsLeft = 0;
                    m.ToxicCounter = 0;
                }
                ev.Battle.Log.Raw($"|-cureteam|{u.Species?.Name ?? u.Nickname}|[from] move: Heal Bell");
                return;
            }
        }
    }
}
