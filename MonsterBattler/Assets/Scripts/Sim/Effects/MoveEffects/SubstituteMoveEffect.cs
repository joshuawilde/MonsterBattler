using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>
    /// Substitute: spend 1/4 max HP to create a decoy with that much HP. Fails if you can't
    /// pay the cost (HP ≤ 1/4) or you already have a substitute.
    /// </summary>
    public sealed class SubstituteMoveEffect : Effect
    {
        public override string EffectId => "substitutemove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var user = ev.User;
            if (user == null || user.IsFainted) return;
            int max = user.MaxStats[(int)Stat.HP];
            int cost = max / 4;
            if (cost <= 0 || user.CurrentHp <= cost)
            {
                ev.Battle.Log.Raw($"|-fail|{user.Species?.Name ?? user.Nickname}|move: Substitute|[weak]");
                return;
            }
            if (user.Volatiles.ContainsKey("substitute"))
            {
                ev.Battle.Log.Raw($"|-fail|{user.Species?.Name ?? user.Nickname}|move: Substitute|[already]");
                return;
            }
            ev.Battle.ApplyDamage(user, cost);
            ev.Battle.Log.Raw($"|-damage|{user.Species?.Name ?? user.Nickname}|{user.CurrentHp}/{max}|[from] Substitute");
            var slot = ev.Battle.AddVolatile(user, "substitute", source: user);
            if (slot != null) slot.Counter = cost;
        }
    }
}
