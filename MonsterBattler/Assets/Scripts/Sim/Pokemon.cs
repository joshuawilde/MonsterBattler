using System.Collections.Generic;
using MonsterBattler.Sim.Data;
using MonsterBattler.Sim.Effects;

namespace MonsterBattler.Sim
{
    /// <summary>
    /// Runtime instance of a Pokemon. Holds current HP, status, stat stages, active moves,
    /// references to species/ability/item data, and the resolved <see cref="Effect"/> instances
    /// the engine dispatches hooks through.
    /// </summary>
    public sealed class Pokemon
    {
        public SpeciesData Species;
        public string Nickname;
        public int Level = 50;
        public Gender Gender;

        // Data (immutable shape) — set at team load.
        public AbilityData Ability;
        public ItemData Item;

        // Resolved effect logic — kept in sync with the data fields above.
        public Effect AbilityEffect;
        public Effect ItemEffect;
        public Effect StatusEffect;
        public readonly Dictionary<string, Effect> Volatiles = new();
        /// <summary>Lightweight string tags for ability/effect flags (e.g. "flashfire"). Cleared on switch out.</summary>
        public readonly HashSet<string> Tags = new();

        public int[] IVs = new int[6];
        public int[] EVs = new int[6];

        public int[] MaxStats = new int[6];
        public int CurrentHp;
        public int[] StatStages = new int[8]; // indexed by Stat enum: HP..Eva. -6..+6 each.

        public StatusCondition Status;
        public int SleepTurnsLeft;
        public int ToxicCounter;

        public MonType TeraType;
        public bool IsTerastallized;

        public List<MoveSlot> Moves = new();
        public bool IsActive;
        public bool IsFainted => CurrentHp <= 0;

        /// <summary>
        /// Walks every effect currently attached to this Pokemon. Stable order: ability →
        /// item → status → volatiles (insertion order). The engine uses this to fan events out.
        /// </summary>
        public IEnumerable<Effect> ActiveEffects()
        {
            if (AbilityEffect != null) yield return AbilityEffect;
            if (ItemEffect != null) yield return ItemEffect;
            if (StatusEffect != null) yield return StatusEffect;
            foreach (var v in Volatiles.Values) yield return v;
        }

        public override string ToString() => $"{Nickname ?? Species?.Name} ({CurrentHp}/{MaxStats[(int)Stat.HP]})";
    }

    public sealed class MoveSlot
    {
        public MoveData Move;
        public int Pp;
        public int MaxPp;
        public bool Disabled;
    }
}
