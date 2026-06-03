using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Metal Burst: returns 1.5× of the latest damage taken this turn, either category.</summary>
    public sealed class MetalBurstMoveEffect : Effect
    {
        public override string EffectId => "metalburstmove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var u = ev.User;
            if (u == null) return;
            if (u.LastDamageTurn != ev.Battle.TurnNumber || u.LastDamageAmount <= 0
                || u.LastDamageSource == null || u.LastDamageSource.IsFainted)
            {
                ev.Battle.Log.Raw($"|-fail|{u.Species?.Name ?? u.Nickname}");
                return;
            }
            int dmg = u.LastDamageAmount * 3 / 2;
            ev.Battle.ApplyDamage(u.LastDamageSource, dmg);
            ev.Battle.Log.Damage(u.LastDamageSource, dmg);
        }
    }
}
