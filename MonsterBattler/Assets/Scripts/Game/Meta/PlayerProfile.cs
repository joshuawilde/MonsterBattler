using System.Collections.Generic;

namespace MonsterBattler.Game.Meta
{
    /// <summary>Persistent player state for the meta loop: collection, team, currency.</summary>
    [System.Serializable]
    public sealed class PlayerProfile
    {
        public List<string> owned = new();   // species ids the player has collected (distinct)
        public List<string> team = new();    // up to TeamSize species ids (a subset of owned)
        public int coins = 0;
        public bool initialized = false;     // false until the first-launch starter grant runs

        public string username = "You";      // shown on the matchmaking / result screens
        public int elo = 1000;               // ranked rating (standard Elo vs calibrated bots)
        public int gamesPlayed = 0;          // for the provisional (high-K) rating period
        public int rev = 0;                  // monotonic save revision (cloud last-write-wins)

        public List<MonMoves> monMoves = new();  // per-species move unlocks / equips
    }

    /// <summary>Per-species progression: move unlocks/equips, plus level growth. Parallel lists
    /// because JsonUtility can't do dictionaries.</summary>
    [System.Serializable]
    public sealed class MonMoves
    {
        public string species;
        public List<string> unlocked = new();     // move ids available to equip (basics + earned)
        public List<string> equipped = new();     // up to 4, subset of unlocked
        public List<string> progressIds = new();  // locked move ids with partial progress…
        public List<int> progressPts = new();     // …and their accumulated points (parallel)

        // Leveling (Showdown-style: level only changes stats via the standard formula).
        // Mons start StartLevelOffset below their randbats level and grow back up to it (the cap —
        // randbats levels ARE the format's balance point, so maxed mons = balanced Showdown mons).
        public int levelsGained = 0;              // whole levels earned so far
        public int xp = 0;                        // progress toward the next level
    }
}
