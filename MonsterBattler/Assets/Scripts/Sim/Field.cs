using System.Collections.Generic;

namespace MonsterBattler.Sim
{
    /// <summary>
    /// Shared battlefield state: weather, terrain, gravity, trick room, etc.
    /// </summary>
    public sealed class Field
    {
        public Weather Weather;
        public int WeatherTurnsLeft;
        public Terrain Terrain;
        public int TerrainTurnsLeft;

        public Dictionary<string, FieldCondition> Conditions = new();
    }

    public sealed class FieldCondition
    {
        public string Id;
        public int TurnsLeft;
        public object Data;
    }
}
