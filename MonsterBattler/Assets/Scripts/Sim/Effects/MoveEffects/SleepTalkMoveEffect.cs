using System.Collections.Generic;
using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Sleep Talk: while asleep, calls a random one of the user's other moves.</summary>
    public sealed class SleepTalkMoveEffect : Effect
    {
        public override string EffectId => "sleeptalkmove";
        static readonly HashSet<string> NotCallable = new() { "sleeptalk", "snore", "metronome", "assist", "mirrormove", "copycat", "mefirst", "bide", "dig", "dive", "fly", "phantomforce", "shadowforce", "solarbeam", "skullbash" };
        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var u = ev.User;
            if (u == null || u.Status != StatusCondition.Sleep) return; // only works while asleep
            var pool = new List<string>();
            foreach (var ms in u.Moves)
                if (ms.Move != null && !NotCallable.Contains(ms.Move.Id)) pool.Add(ms.Move.Id);
            if (pool.Count == 0) return;
            var pick = pool[ev.Battle.Prng.Range(0, pool.Count)];
            ev.Battle.CallMove(u, ev.Target, pick);
        }
    }
}
