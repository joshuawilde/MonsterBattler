namespace MonsterBattler.Sim.Effects
{
    /// <summary>
    /// A volatile (temporary, per-Pokemon) effect attachment. Effects are stateless singletons,
    /// so per-instance data — who applied it, turns remaining, layer counts, secondary payloads —
    /// lives here on the slot itself.
    ///
    /// Examples:
    ///   • Leech Seed:    Effect = LeechSeedVolatile,  Source = the seeder
    ///   • Protect:       Effect = ProtectVolatile,    SingleTurn = true
    ///   • Confusion:     Effect = ConfusionVolatile,  Turns = 2..5 (counts down)
    ///   • Toxic counter: Effect = ToxicVolatile,      Counter = damage multiplier
    /// </summary>
    public sealed class VolatileSlot
    {
        public Effect Effect;
        /// <summary>The Pokemon that applied this volatile, if relevant (Leech Seed, Bind, etc.).</summary>
        public Pokemon Source;
        /// <summary>Turns remaining; -1 means "no duration tracking, lifetime managed by other rules".</summary>
        public int Turns = -1;
        /// <summary>Generic counter (Toxic damage multiplier, Stockpile layers, etc.).</summary>
        public int Counter;
        /// <summary>If true, the slot is automatically removed at end of turn (Protect/Detect/etc.).</summary>
        public bool SingleTurn;
        /// <summary>Per-volatile freeform payload — keep this rare; prefer named fields above.</summary>
        public object Extra;
    }
}
