using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Endeavor: reduces target HP to match user HP. Fails if user has more HP than target.</summary>
    public sealed class EndeavorMoveEffect : Effect
    {
        public override string EffectId => "endeavormove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var u = ev.User; var t = ev.Target;
            if (u == null || t == null || t.IsFainted) return;
            if (u.CurrentHp >= t.CurrentHp)
            {
                ev.Battle.Log.Raw($"|-fail|{u.Species?.Name ?? u.Nickname}");
                return;
            }
            int dmg = t.CurrentHp - u.CurrentHp;
            ev.Battle.ApplyDamage(t, dmg);
            ev.Battle.Log.Damage(t, dmg);
        }
    }
}
