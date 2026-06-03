using System.Collections.Generic;
using MonsterBattler.Sim;

namespace MonsterBattler.Game.AI
{
    /// <summary>
    /// Port of Pokemon Showdown's <c>RandomPlayerAI</c> (sim/tools/random-player-ai.ts).
    /// It is intentionally a baseline, not a strong bot:
    ///
    /// • On a normal turn: enumerate usable moves and valid switches. If both are available,
    ///   switch with probability <c>(1 - move)</c>; otherwise pick a uniformly-random move.
    ///   Default <c>move = 1.0</c> means "always attack, never voluntarily switch" — matching PS.
    /// • Move pick: uniform sample over non-disabled moves.
    /// • Switch pick: uniform sample over non-active, non-fainted bench mons.
    /// • Forced switches (after a faint) are handled separately by the engine for now;
    ///   when we surface those to the player, this AI will get a hook too.
    ///
    /// Use a seeded constructor for reproducible runs; the parameterless ctor falls back to the
    /// battle's own PRNG so we still inherit determinism from the seed at <see cref="Battle"/>.
    /// </summary>
    public sealed class RandomPlayerAI : IBattleAI
    {
        /// <summary>Probability of picking a move when both move and switch are valid (0..1).</summary>
        public float Move = 1.0f;

        readonly Prng _prng;

        public RandomPlayerAI() { _prng = null; }
        public RandomPlayerAI(ulong seed) { _prng = new Prng(seed); }
        public RandomPlayerAI(float move, ulong? seed = null)
        {
            Move = move;
            _prng = seed.HasValue ? new Prng(seed.Value) : null;
        }

        public Choice ChooseAction(Battle battle, Side ownSide, Side opponentSide)
        {
            var rng = _prng ?? battle.Prng;
            var active = ownSide.ActiveSlots.Count > 0 ? ownSide.ActiveSlots[0] : null;
            if (active == null || active.IsFainted)
            {
                // No active mon to act with — the engine will auto-switch from the bench.
                // Surface a no-op move choice; engine handles the rest.
                return Choice.UseMove(FirstUsableMoveId(active) ?? "tackle");
            }

            // Usable moves (PS doesn't filter on PP > 0; some moves are needed at 0 PP via
            // edge cases. Mirror that here — non-disabled is the only check.)
            var canMove = new List<int>();
            for (int i = 0; i < active.Moves.Count; i++)
                if (!active.Moves[i].Disabled) canMove.Add(i);

            // Valid switches.
            var canSwitch = new List<int>();
            for (int i = 0; i < ownSide.Team.Count; i++)
            {
                var p = ownSide.Team[i];
                if (p == null || p == active || p.IsFainted) continue;
                canSwitch.Add(i);
            }

            // Decide.
            bool wantsMove = canMove.Count > 0 && (canSwitch.Count == 0 || rng.NextFloat() <= Move);
            if (wantsMove)
            {
                var pick = canMove[rng.Range(0, canMove.Count)];
                return Choice.UseMove(active.Moves[pick].Move.Id);
            }
            if (canSwitch.Count > 0)
            {
                var pick = canSwitch[rng.Range(0, canSwitch.Count)];
                return Choice.SwitchTo(pick);
            }

            // Pathological case (no moves and no switches): fall back to first move slot.
            return Choice.UseMove(active.Moves.Count > 0 ? active.Moves[0].Move.Id : "tackle");
        }

        static string FirstUsableMoveId(Pokemon mon)
        {
            if (mon == null) return null;
            foreach (var m in mon.Moves) if (!m.Disabled) return m.Move.Id;
            return null;
        }
    }
}
