using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MonsterBattler.Game.Meta
{
    /// <summary>
    /// The meta layer: loads/saves the <see cref="PlayerProfile"/>, runs the gacha pull, and
    /// applies battle rewards. Pure orchestration over a JSON save file — the battle engine is
    /// untouched. Static so any scene (menu or battle) shares one profile.
    /// </summary>
    public static class MetaGame
    {
        public const int PullCost = 100;
        public const int WinReward = 120;
        public const int LossReward = 30;
        public const int StarterMonsters = 8;
        public const int StarterCoins = 300;
        public const int TeamSize = 4;

        // Rarity tiers, by base-stat total. Index 0..3 = Common/Rare/Epic/Legendary.
        public static readonly string[] RarityNames = { "Common", "Rare", "Epic", "Legendary" };
        static readonly int[] RarityWeights = { 60, 27, 10, 3 };

        static PlayerProfile _profile;
        static List<string> _pool;                 // all summonable species ids
        static Sim.Data.Dex _dex;                  // for base stats (rarity) + names
        static Dictionary<int, List<string>> _byTier;

        static string SavePath => System.IO.Path.Combine(Application.persistentDataPath, "profile.json");

        public static PlayerProfile Profile => _profile ??= Load();
        public static IReadOnlyList<string> Pool => _pool ??= LoadPool();
        public static Sim.Data.Dex Dex => _dex ??= SafeLoadDex();

        static Sim.Data.Dex SafeLoadDex() { try { return DexLoader.LoadFromStreamingAssets(); } catch { return null; } }

        static List<string> LoadPool()
        {
            try { return RandbatsLoader.LoadFromStreamingAssets().Species.Keys.OrderBy(k => k).ToList(); }
            catch { return new List<string>(); }
        }

        /// <summary>Rarity tier (0..3) of a species, derived from its base-stat total.</summary>
        public static int Rarity(string speciesId)
        {
            if (Dex != null && Dex.Species.TryGetValue(speciesId, out var sp))
            {
                int bst = sp.BaseStats.Total;
                return bst >= 580 ? 3 : bst >= 525 ? 2 : bst >= 480 ? 1 : 0;
            }
            return 0;
        }

        static PlayerProfile Load()
        {
            PlayerProfile p = null;
            try
            {
                if (System.IO.File.Exists(SavePath))
                    p = JsonUtility.FromJson<PlayerProfile>(System.IO.File.ReadAllText(SavePath));
            }
            catch { p = null; }
            p ??= new PlayerProfile();
            if (!p.initialized) GrantStarter(p);
            return p;
        }

        public static void Save()
        {
            try { System.IO.File.WriteAllText(SavePath, JsonUtility.ToJson(_profile)); }
            catch (System.Exception e) { Debug.LogWarning($"[MetaGame] save failed: {e.Message}"); }
        }

        static void GrantStarter(PlayerProfile p)
        {
            var pool = Pool;
            var rng = new System.Random();
            var picked = pool.OrderBy(_ => rng.Next()).Take(StarterMonsters).ToList();
            p.owned = picked.Distinct().ToList();
            p.team = p.owned.Take(TeamSize).ToList();
            p.coins = StarterCoins;
            p.initialized = true;
            _profile = p;
            Save();
        }

        /// <summary>Spend coins to summon one random monster. Returns the species id, or null if too poor.
        /// A duplicate refunds half the pull cost instead.</summary>
        static Dictionary<int, List<string>> ByTier()
        {
            if (_byTier != null) return _byTier;
            _byTier = new Dictionary<int, List<string>> { { 0, new() }, { 1, new() }, { 2, new() }, { 3, new() } };
            foreach (var id in Pool) _byTier[Rarity(id)].Add(id);
            return _byTier;
        }

        public static string Pull(out bool duplicate)
        {
            duplicate = false;
            var p = Profile;
            if (p.coins < PullCost || Pool.Count == 0) return null;
            p.coins -= PullCost;
            var rng = new System.Random();

            // Pick a rarity tier by weight (skipping empty tiers), then a random species within it.
            var tiers = ByTier();
            int total = 0;
            for (int t = 0; t < 4; t++) if (tiers[t].Count > 0) total += RarityWeights[t];
            int roll = rng.Next(total), chosenTier = 0;
            for (int t = 0; t < 4; t++)
            {
                if (tiers[t].Count == 0) continue;
                if (roll < RarityWeights[t]) { chosenTier = t; break; }
                roll -= RarityWeights[t];
            }
            var bucket = tiers[chosenTier];
            string id = bucket[rng.Next(bucket.Count)];
            if (p.owned.Contains(id))
            {
                duplicate = true;
                p.coins += PullCost / 2; // pity refund
            }
            else
            {
                p.owned.Add(id);
                if (p.team.Count < TeamSize) p.team.Add(id);
            }
            Save();
            return id;
        }

        /// <summary>The team to take into battle: the saved team, falling back to the first owned mons.</summary>
        public static List<string> BattleTeam()
        {
            var p = Profile;
            var t = p.team.Where(id => p.owned.Contains(id)).Take(TeamSize).ToList();
            if (t.Count == 0) t = p.owned.Take(TeamSize).ToList();
            return t;
        }

        public static void SetTeam(IEnumerable<string> ids)
        {
            Profile.team = ids.Where(id => Profile.owned.Contains(id)).Distinct().Take(TeamSize).ToList();
            Save();
        }

        /// <summary>Apply the coin reward for a finished battle (playerWon true = win bonus).</summary>
        public static int Reward(bool playerWon)
        {
            int r = playerWon ? WinReward : LossReward;
            Profile.coins += r;
            Save();
            return r;
        }
    }
}
