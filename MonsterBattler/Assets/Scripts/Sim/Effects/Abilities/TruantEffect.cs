using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Truant: the owner can only act every other turn. After a turn in which it executes a move,
    /// it "loafs around" the following turn and its move is cancelled.
    /// </summary>
    public sealed class TruantEffect : Effect
    {
        public override string EffectId => "truant";
        public override string DisplayName => "Truant";

        public override void OnBeforeMove(BeforeMoveEvent ev, Pokemon owner)
        {
            if (owner != ev.User) return;

            var slot = owner.GetVolatile("truant");
            if (slot != null && slot.Counter == 1)
            {
                // Loafing this turn: clear the flag and skip the move.
                ev.Battle.RemoveVolatile(owner, "truant");
                ev.Battle.Log.Raw($"|cant|{owner.Species?.Name ?? owner.Nickname}|ability: Truant");
                ev.Cancelled = true;
                ev.CancelReason = "Truant";
                return;
            }

            // Acted this turn: schedule loafing next turn.
            var s = ev.Battle.AddVolatile(owner, "truant", source: owner);
            if (s != null) s.Counter = 1;
        }
    }
}
