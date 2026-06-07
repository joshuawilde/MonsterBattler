using System;
using MonsterBattler.Sim;

namespace MonsterBattler.Game.AI
{
    /// <summary>
    /// Builds an opponent AI for a target (self-consistent) Elo on ONE smooth scale. The heuristic +
    /// temperature dial (<see cref="EloBattleAI"/>) covers ≈871–1262 continuously; above that, the
    /// measured search rungs continue the ladder to ≈1514. Calibrated by tools/calibrate-ai `unified`.
    /// </summary>
    public static class BattleAIFactory
    {
        /// <summary>Below this Elo, use the continuous heuristic-temperature dial; at/above, search rungs.</summary>
        public const int SearchFloor = 1300;

        // Measured search rungs (elo, builder), ascending. One Elo scale with EloBattleAI._curve.
        static readonly (int elo, Func<ulong?, IBattleAI> build)[] _searchRungs =
        {
            (1349, s => new SearchAI(depth: 1, topMy: 3, topOpp: 2, samples: 1, seed: s)),
            (1406, s => new SearchAI(depth: 1, seed: s)),
            (1470, s => new SearchAI(depth: 2, seed: s)),
        };

        public static IBattleAI ForElo(int elo, ulong? seed = null)
        {
            if (elo < SearchFloor) return new EloBattleAI(elo, seed); // continuous heuristic band

            var best = _searchRungs[0];
            int bestDist = Math.Abs(elo - best.elo);
            foreach (var r in _searchRungs)
            {
                int d = Math.Abs(elo - r.elo);
                if (d < bestDist) { bestDist = d; best = r; }
            }
            return best.build(seed);
        }
    }
}
