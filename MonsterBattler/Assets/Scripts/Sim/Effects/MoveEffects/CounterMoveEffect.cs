using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Counter: doubles the most recent physical damage the user took this turn back at the source.</summary>
    public sealed class CounterMoveEffect : Effect
    {
        public override string EffectId => "countermove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var u = ev.User;
            if (u == null) return;
            if (u.LastDamageTurn != ev.Battle.TurnNumber || u.LastDamageAmount <= 0
                || u.LastDamageCategory != MoveCategory.Physical
                || u.LastDamageSource == null || u.LastDamageSource.IsFainted)
            {
                ev.Battle.Log.Raw($"|-fail|{u.Species?.Name ?? u.Nickname}");
                return;
            }
            int dmg = u.LastDamageAmount * 2;
            ev.Battle.ApplyDamage(u.LastDamageSource, dmg);
            ev.Battle.Log.Damage(u.LastDamageSource, dmg);
        }
    }
}
