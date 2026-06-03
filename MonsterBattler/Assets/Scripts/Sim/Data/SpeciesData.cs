using System.Collections.Generic;

namespace MonsterBattler.Sim.Data
{
    /// <summary>
    /// Immutable species definition. Loaded from JSON, never mutated at runtime.
    /// One per Pokemon species (Pikachu, Charizard, etc.).
    /// </summary>
    public sealed class SpeciesData
    {
        public string Id;            // e.g. "pikachu" — matches PS id
        public string Name;          // display name
        public MonType Type1;
        public MonType Type2;        // MonType.None if mono-type
        public BaseStats BaseStats;
        public IReadOnlyList<string> AbilityIds = System.Array.Empty<string>(); // includes hidden
        public float WeightKg;
        public float HeightM;
        public Gender GenderRatioMale;   // -1 for genderless, otherwise male probability
        // TODO: evolution chain, learnset reference, base experience, egg groups.
    }

    public struct BaseStats
    {
        public int HP, Atk, Def, SpA, SpD, Spe;
        public int Total => HP + Atk + Def + SpA + SpD + Spe;
    }
}
