using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Volatiles
{
    /// <summary>Gulp Missile: after Surf/Dive, Cramorant spits at the next attacker (damage + Def drop), then reverts.</summary>
    public sealed class GulpMissileVolatile : Effect
    {
        public override string EffectId => "gulpmissilecharge";
        public override string DisplayName => "Gulp Missile";
        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            var atk = ev.User;
            ev.Battle.RemoveVolatile(owner, "gulpmissilecharge");
            if (atk == null || atk.IsFainted) return;
            ev.Battle.ApplyDamage(atk, System.Math.Max(1, atk.MaxStats[(int)Stat.HP] / 4), DamageSource.Other);
            ev.Battle.Log.Raw($"|-damage|{atk.Species?.Name ?? atk.Nickname}|{atk.CurrentHp}/{atk.MaxStats[(int)Stat.HP]}|[from] ability: Gulp Missile");
            ev.Battle.BoostStat(atk, Stat.Def, -1, owner);
        }
    }
}
