using System.Collections.Generic;

namespace MonsterBattler.Sim
{
    /// <summary>
    /// One side of the battle (a "trainer"). For singles, ActiveSlots has one entry.
    /// Doubles/triples expand without engine changes — that's what the array shape is for.
    /// </summary>
    public sealed class Side
    {
        public int Index;           // 0 (player) or 1 (opponent) for singles
        public string Name;
        public List<Pokemon> Team = new();
        public List<Pokemon> ActiveSlots = new();

        // Side conditions: Reflect, Light Screen, Spikes, etc. — TODO when wiring conditions.
        public Dictionary<string, SideCondition> Conditions = new();
    }

    public sealed class SideCondition
    {
        public string Id;
        public int TurnsLeft;
        public int Layers;          // for Spikes/Toxic Spikes
        public object Data;         // condition-specific payload
    }
}
