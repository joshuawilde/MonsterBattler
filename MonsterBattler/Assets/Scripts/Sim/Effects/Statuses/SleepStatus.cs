using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Statuses
{
    /// <summary>
    /// Sleep: each turn, before the move, decrement <see cref="Pokemon.SleepTurnsLeft"/>. When
    /// the counter hits 0 the Pokemon wakes up and acts normally; until then, the move is cancelled.
    /// Initial counter is set in <see cref="Battle.ApplyStatus"/>.
    /// </summary>
    public sealed class SleepStatus : Effect
    {
        public override string EffectId => "slp";
        public override string DisplayName => "Sleep";

        public override void OnBeforeMove(BeforeMoveEvent ev, Pokemon owner)
        {
            if (owner != ev.User) return;
            if (ev.Battle.CallingMove) return; // a move invoked by Sleep Talk bypasses the sleep gate
            if (owner.SleepTurnsLeft <= 0)
            {
                owner.Status = StatusCondition.None;
                owner.StatusEffect = null;
                ev.Battle.Log.Raw($"|-curestatus|{owner.Species?.Name ?? owner.Nickname}|slp");
                return;
            }
            // Early Bird wakes up twice as fast.
            owner.SleepTurnsLeft -= owner.AbilityEffect is Abilities.EarlyBirdEffect ? 2 : 1;
            if (owner.SleepTurnsLeft <= 0)
            {
                owner.Status = StatusCondition.None;
                owner.StatusEffect = null;
                ev.Battle.Log.Raw($"|-curestatus|{owner.Species?.Name ?? owner.Nickname}|slp");
                return;
            }
            // Sleep Talk and Snore are usable while asleep.
            if (ev.Move != null && (ev.Move.Id == "sleeptalk" || ev.Move.Id == "snore")) return;
            ev.Battle.Log.Raw($"|cant|{owner.Species?.Name ?? owner.Nickname}|slp");
            ev.Cancelled = true;
        }
    }
}
