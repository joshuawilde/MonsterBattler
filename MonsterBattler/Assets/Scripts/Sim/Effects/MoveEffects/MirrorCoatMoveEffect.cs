using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Mirror Coat: special-damage analogue of Counter — doubles the latest special hit back.</summary>
    public sealed class MirrorCoatMoveEffect : Effect
    {
        public override string EffectId => "mirrorcoatmove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var u = ev.User;
            if (u == null) return;
            if (u.LastDamageTurn != ev.Battle.TurnNumber || u.LastDamageAmount <= 0
                || u.LastDamageCategory != MoveCategory.Special
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
