using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Night Shade / Seismic Toss: damage equals user's level. Ghost-type immunity is respected for Night Shade only by setting move type Ghost in JSON.</summary>
    public sealed class LevelDamageMoveEffect : Effect
    {
        public override string EffectId => "leveldamagemove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            if (ev.Target == null || ev.Target.IsFainted || ev.User == null) return;
            ev.Battle.ApplyDamage(ev.Target, ev.User.Level);
            ev.Battle.Log.Damage(ev.Target, ev.User.Level);
        }
    }

    /// <summary>Sonic Boom: fixed 20 damage.</summary>
    public sealed class SonicBoomMoveEffect : Effect
    {
        public override string EffectId => "sonicboommove";
        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            if (ev.Target == null || ev.Target.IsFainted) return;
            ev.Battle.ApplyDamage(ev.Target, 20);
            ev.Battle.Log.Damage(ev.Target, 20);
        }
    }

    /// <summary>Dragon Rage: fixed 40 damage.</summary>
    public sealed class DragonRageMoveEffect : Effect
    {
        public override string EffectId => "dragonragemove";
        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            if (ev.Target == null || ev.Target.IsFainted) return;
            ev.Battle.ApplyDamage(ev.Target, 40);
            ev.Battle.Log.Damage(ev.Target, 40);
        }
    }

    /// <summary>Final Gambit: deals damage equal to user's current HP, user faints.</summary>
    public sealed class FinalGambitMoveEffect : Effect
    {
        public override string EffectId => "finalgambitmove";
        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var u = ev.User; var t = ev.Target;
            if (u == null || t == null || t.IsFainted) return;
            int dmg = u.CurrentHp;
            ev.Battle.ApplyDamage(t, dmg);
            ev.Battle.Log.Damage(t, dmg);
            ev.Battle.ApplyDamage(u, u.CurrentHp);
            ev.Battle.Log.Faint(u);
        }
    }

    /// <summary>Memento: user faints, lowers target's Atk and SpA by 2 stages.</summary>
    public sealed class MementoMoveEffect : Effect
    {
        public override string EffectId => "mementomove";
        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var u = ev.User; var t = ev.Target;
            if (u == null || t == null) return;
            ev.Battle.BoostStat(t, Stat.Atk, -2, source: u);
            ev.Battle.BoostStat(t, Stat.SpA, -2, source: u);
            ev.Battle.ApplyDamage(u, u.CurrentHp);
            ev.Battle.Log.Faint(u);
        }
    }

    /// <summary>Healing Wish: user faints, the next mon brought in is fully healed and status-cured.</summary>
    public sealed class HealingWishMoveEffect : Effect
    {
        public override string EffectId => "healingwishmove";
        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var u = ev.User;
            if (u == null || u.IsFainted) return;
            var side = ev.Battle.SideOf(u);
            if (side == null) return;
            var c = ev.Battle.AddSideCondition(side, "healingwish", maxLayers: 1);
            if (c != null) c.TurnsLeft = -1; // perpetual until consumed
            ev.Battle.ApplyDamage(u, u.CurrentHp);
            ev.Battle.Log.Faint(u);
        }
    }
}
