using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Pain Split: averages user and target current HP (capped at each side's max).</summary>
    public sealed class PainSplitMoveEffect : Effect
    {
        public override string EffectId => "painsplitmove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var u = ev.User; var t = ev.Target;
            if (u == null || t == null || u.IsFainted || t.IsFainted) return;
            int avg = (u.CurrentHp + t.CurrentHp) / 2;
            u.CurrentHp = System.Math.Min(avg, u.MaxStats[(int)Stat.HP]);
            t.CurrentHp = System.Math.Min(avg, t.MaxStats[(int)Stat.HP]);
            ev.Battle.Log.Raw($"|-sethp|{u.Species?.Name ?? u.Nickname}|{u.CurrentHp}/{u.MaxStats[(int)Stat.HP]}|[from] move: Pain Split");
            ev.Battle.Log.Raw($"|-sethp|{t.Species?.Name ?? t.Nickname}|{t.CurrentHp}/{t.MaxStats[(int)Stat.HP]}|[silent]");
        }
    }
}
