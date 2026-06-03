using System.Collections.Generic;

namespace MonsterBattler.Sim.Data
{
    /// <summary>
    /// Loaded once at startup, queried by id. The Unity layer fills this from JSON in StreamingAssets;
    /// the sim never reads files itself (keeps the sim asmdef pure C# with zero IO dependencies).
    /// </summary>
    public sealed class Dex
    {
        public readonly Dictionary<string, SpeciesData> Species = new();
        public readonly Dictionary<string, MoveData> Moves = new();
        public readonly Dictionary<string, AbilityData> Abilities = new();
        public readonly Dictionary<string, ItemData> Items = new();

        public SpeciesData Get(string id) => Species[id];
        public MoveData GetMove(string id) => Moves[id];
        public AbilityData GetAbility(string id) => Abilities[id];
        public ItemData GetItem(string id) => Items[id];
    }
}
