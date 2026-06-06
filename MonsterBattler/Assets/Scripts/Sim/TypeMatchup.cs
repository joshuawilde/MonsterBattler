using System.Collections.Generic;

namespace MonsterBattler.Sim
{
    /// <summary>
    /// Computes a Pokemon's defensive type matchup — which attacking types hit it for ×4 / ×2 /
    /// ×0.5 / ×0.25 / ×0 — the way Showdown's tooltip shows weaknesses and resistances. Pure
    /// function of the defender's type(s), so it's unit-testable with no battle state.
    /// </summary>
    public static class TypeMatchup
    {
        /// <summary>The 18 real attacking types (excludes None and the Tera-only Stellar type).</summary>
        public static readonly MonType[] AttackingTypes =
        {
            MonType.Normal, MonType.Fire, MonType.Water, MonType.Electric, MonType.Grass, MonType.Ice,
            MonType.Fighting, MonType.Poison, MonType.Ground, MonType.Flying, MonType.Psychic,
            MonType.Bug, MonType.Rock, MonType.Ghost, MonType.Dragon, MonType.Dark, MonType.Steel, MonType.Fairy,
        };

        /// <summary>An attacking type and the multiplier it deals to the defender.</summary>
        public readonly struct Entry
        {
            public readonly MonType Type;
            public readonly float Multiplier;
            public Entry(MonType type, float mult) { Type = type; Multiplier = mult; }
        }

        /// <summary>
        /// All non-neutral matchups against a defender, ordered most-effective first
        /// (×4, ×2, ×0.5, ×0.25, ×0). Neutral (×1) types are omitted.
        /// </summary>
        public static List<Entry> Defensive(MonType type1, MonType type2)
        {
            var list = new List<Entry>();
            foreach (var atk in AttackingTypes)
            {
                float m = TypeChart.Effectiveness(atk, type1, type2);
                if (m != 1f) list.Add(new Entry(atk, m));
            }
            // Descending by multiplier so weaknesses come first, immunities last.
            list.Sort((a, b) => b.Multiplier.CompareTo(a.Multiplier));
            return list;
        }
    }
}
