using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Volatiles
{
    /// <summary>
    /// Confusion: each turn before the move, 33% chance to hit yourself with a typeless 40-BP
    /// physical attack using own Atk vs own Def. Duration tracked on <see cref="VolatileSlot.Turns"/>.
    /// </summary>
    public sealed class ConfusionVolatile : Effect
    {
        public override string EffectId => "confusion";
        public override string DisplayName => "Confusion";

        public override void OnBeforeMove(BeforeMoveEvent ev, Pokemon owner)
        {
            if (owner != ev.User) return;
            var slot = owner.GetVolatile("confusion");
            if (slot == null) return;

            if (slot.Turns <= 0)
            {
                ev.Battle.RemoveVolatile(owner, "confusion");
                return;
            }
            slot.Turns--;

            if (!ev.Battle.Prng.Chance(1, 3)) return;

            ev.Battle.Log.Raw($"|-activate|{owner.Species?.Name ?? owner.Nickname}|confusion");
            int dmg = SelfHitDamage(ev.Battle, owner);
            ev.Battle.ApplyDamage(owner, dmg);
            ev.Battle.Log.Raw($"|-damage|{owner.Species?.Name ?? owner.Nickname}|{owner.CurrentHp}/{owner.MaxStats[(int)Stat.HP]}|[from] confusion");
            ev.Cancelled = true;
        }

        static int SelfHitDamage(Battle battle, Pokemon user)
        {
            int level = user.Level;
            int atk = user.MaxStats[(int)Stat.Atk];
            int def = user.MaxStats[(int)Stat.Def];
            int dmg = (int)System.Math.Floor((2.0 * level / 5.0 + 2.0) * 40 * atk / def);
            dmg = (int)System.Math.Floor(dmg / 50.0) + 2;
            int roll = battle.Prng.Range(85, 101);
            return System.Math.Max(1, dmg * roll / 100);
        }
    }
}
