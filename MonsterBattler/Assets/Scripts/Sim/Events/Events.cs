using MonsterBattler.Sim.Data;

namespace MonsterBattler.Sim.Events
{
    /// <summary>
    /// Base class for everything the engine dispatches through effect hooks.
    /// Modifier events expose mutable fields that listeners modify in-place;
    /// notification events are read-only side-effect carriers.
    /// </summary>
    public abstract class BattleEvent
    {
        public Battle Battle;
    }

    /* ------------------------------------------------------------------ */
    /* Move pipeline                                                       */
    /* ------------------------------------------------------------------ */

    /// <summary>
    /// Fires after PP gate, before targeting / accuracy / damage. Listeners (Sleep, Confusion,
    /// Paralysis full-para roll, Truant, Recharge, etc.) can <see cref="Cancelled"/> = true to
    /// abort the move attempt entirely.
    /// </summary>
    public sealed class BeforeMoveEvent : BattleEvent
    {
        public Pokemon User;
        public MoveData Move;
        public bool Cancelled;
        public string CancelReason;
    }

    public sealed class TryHitEvent : BattleEvent
    {
        public Pokemon User;
        public Pokemon Target;
        public MoveData Move;
        /// <summary>A listener sets this to mark the move as blocked (e.g. Levitate vs Ground).</summary>
        public bool Blocked;
        public string BlockReason;
    }

    /// <summary>
    /// Accuracy modifier event. Listeners scale the rolled-out accuracy% before the hit check.
    /// Compound Eyes pushes it up; Sand Veil / Snow Cloak push it down.
    /// </summary>
    public sealed class ModifyAccuracyEvent : BattleEvent
    {
        public Pokemon User;
        public Pokemon Target;
        public MoveData Move;
        /// <summary>Out of 100, mutable.</summary>
        public int Accuracy;
    }

    public sealed class HitEvent : BattleEvent
    {
        public Pokemon User;
        public Pokemon Target;
        public MoveData Move;
        public int DamageDealt;
    }

    public sealed class AfterMoveEvent : BattleEvent
    {
        public Pokemon User;
        public Pokemon Target;
        public MoveData Move;
        public int DamageDealt;
    }

    /* ------------------------------------------------------------------ */
    /* Damage calc — modifier events                                       */
    /* ------------------------------------------------------------------ */

    public sealed class BasePowerEvent : BattleEvent
    {
        public Pokemon User;
        public Pokemon Target;
        public MoveData Move;
        /// <summary>Mutable. Listeners scale/replace as needed (Blaze multiplies by 1.5, etc.).</summary>
        public int BasePower;
    }

    /// <summary>Lets abilities adjust a move's priority before turn ordering (Prankster, Gale Wings, Triage).</summary>
    public sealed class ModifyPriorityEvent : BattleEvent
    {
        public Pokemon User;
        public MoveData Move;
        public int Priority;
    }

    /// <summary>
    /// Type-changing abilities (Pixilate / Aerilate / Galvanize / Refrigerate, etc.) listen
    /// for this and rewrite the move's effective Type for the rest of the calc, plus add a
    /// BP bonus.
    /// </summary>
    public sealed class ModifyTypeEvent : BattleEvent
    {
        public Pokemon User;
        public MoveData Move;
        public MonType Type;
        /// <summary>Percentage bonus added to base power (20 → ×1.2).</summary>
        public int BasePowerBonus;
    }

    public sealed class StatModifyEvent : BattleEvent
    {
        public Pokemon Owner;
        public Stat Stat;
        public int Value;
        /// <summary>Optional. Set when modification happens in the context of a specific move.</summary>
        public MoveData ContextMove;
    }

    public sealed class ModifyDamageEvent : BattleEvent
    {
        public Pokemon User;
        public Pokemon Target;
        public MoveData Move;
        public int Damage;
        /// <summary>True if this hit was a critical hit — screens skip halving on crits.</summary>
        public bool IsCrit;
    }

    /* ------------------------------------------------------------------ */
    /* Lifecycle                                                           */
    /* ------------------------------------------------------------------ */

    public sealed class SwitchInEvent : BattleEvent
    {
        public Pokemon Pokemon;
        public Pokemon Source; // the mon that landed the KO, if any
    }

    public sealed class SwitchOutEvent : BattleEvent
    {
        public Pokemon Pokemon;
        public Pokemon Source; // the mon that landed the KO, if any
    }

    public sealed class FaintEvent : BattleEvent
    {
        public Pokemon Pokemon;
        public Pokemon Source; // the mon that landed the KO, if any
    }

    /// <summary>
    /// Fires inside <see cref="Battle.ApplyStatus"/> before the status is applied. Listeners
    /// (Limber, Insomnia, Immunity, Magma Armor, Vital Spirit, etc.) set Blocked=true to refuse.
    /// </summary>
    public sealed class TryStatusEvent : BattleEvent
    {
        public Pokemon Target;
        public StatusCondition Status;
        public bool Blocked;
        public string BlockReason;
    }

    public sealed class ResidualEvent : BattleEvent
    {
        public Pokemon Target;
    }
}
