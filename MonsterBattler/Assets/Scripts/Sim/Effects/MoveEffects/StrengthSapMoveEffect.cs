using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Strength Sap: heals the user by the target's current Atk stat value, then lowers
    /// the target's Atk by 1.</summary>
    public sealed class StrengthSapMoveEffect : Effect
    {
        public override string EffectId => "strengthsapmove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var u = ev.User;
            var t = ev.Target;
            if (u == null || u.IsFainted || t == null) return;

            int amount = t.MaxStats[(int)Stat.Atk];
            int max = u.MaxStats[(int)Stat.HP];
            int heal = System.Math.Min(amount, max - u.CurrentHp);
            if (heal > 0)
            {
                u.CurrentHp += heal;
                ev.Battle.Log.Raw($"|-heal|{Name(u)}|{u.CurrentHp}/{max}|[from] move: Strength Sap");
            }

            ev.Battle.BoostStat(t, Stat.Atk, -1, u);
        }

        static string Name(Pokemon m) => m?.Species?.Name ?? m?.Nickname ?? "?";
    }
}
