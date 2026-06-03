using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Recover / Slack Off / Soft-Boiled / Milk Drink: heal 1/2 max HP.</summary>
    public sealed class RecoverMoveEffect : Effect
    {
        public override string EffectId => "recovermove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var u = ev.User;
            if (u == null || u.IsFainted) return;
            int max = u.MaxStats[(int)Stat.HP];
            int heal = System.Math.Min(max / 2, max - u.CurrentHp);
            if (heal <= 0)
            {
                ev.Battle.Log.Raw($"|-fail|{u.Species?.Name ?? u.Nickname}|heal");
                return;
            }
            u.CurrentHp += heal;
            ev.Battle.Log.Raw($"|-heal|{u.Species?.Name ?? u.Nickname}|{u.CurrentHp}/{max}");
        }
    }
}
