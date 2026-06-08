using System.Collections.Generic;

namespace MonsterBattler.Game.Meta
{
    /// <summary>Persistent player state for the meta loop: collection, team, currency.</summary>
    [System.Serializable]
    public sealed class PlayerProfile
    {
        public List<string> owned = new();   // species ids the player has collected (distinct)
        public List<string> team = new();    // up to 6 species ids (a subset of owned)
        public int coins = 0;
        public bool initialized = false;     // false until the first-launch starter grant runs
    }
}
