using System.Collections.Generic;
using MonsterBattler.Sim.Effects;

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
        /// <summary>True once any mon on this side has Terastallized — one tera per battle.</summary>
        public bool HasUsedTera;

        /// <summary>Persistent side conditions (hazards, screens, Tailwind, Mist, Wish).</summary>
        public Dictionary<string, SideCondition> Conditions = new();
    }

    /// <summary>
    /// One side condition slot. Effect carries the callback logic; the slot carries the
    /// per-instance state (turns remaining, layer count, payload).
    /// </summary>
    public sealed class SideCondition
    {
        public string Id;
        public Effect Effect;
        public int TurnsLeft;
        public int Layers;
        public object Data;
    }
}
