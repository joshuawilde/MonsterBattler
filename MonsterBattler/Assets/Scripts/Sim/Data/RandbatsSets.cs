using System.Collections.Generic;

namespace MonsterBattler.Sim.Data
{
    /// <summary>
    /// One curated random-battle set for a species: a role plus the pool of moves/abilities/tera
    /// types it draws from. Mirrors an entry in Pokemon Showdown's gen9 <c>random-battles/sets.json</c>.
    /// The generator samples a 4-move set out of <see cref="MovepoolIds"/> (see RandomTeamGenerator).
    /// </summary>
    public sealed class RandbatsSet
    {
        public string Role;
        public List<string> MovepoolIds = new();   // resolved move ids
        public List<string> AbilityIds = new();     // resolved ability ids
        public List<MonType> TeraTypes = new();
    }

    /// <summary>All curated sets for a species, plus its balance-tuned level.</summary>
    public sealed class RandbatsSpecies
    {
        public int Level;
        public List<RandbatsSet> Sets = new();
    }

    /// <summary>
    /// The full gen9 random-battle dataset, keyed by species id (matches <see cref="SpeciesData.Id"/>).
    /// Loaded from StreamingAssets/dex/randbats.json by the Game layer; the sim never touches IO.
    /// </summary>
    public sealed class RandbatsDex
    {
        public readonly Dictionary<string, RandbatsSpecies> Species = new();
        public bool Has(string id) => Species.ContainsKey(id);
    }
}
