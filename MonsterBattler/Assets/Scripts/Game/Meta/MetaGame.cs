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
        public const int StarterMonsters = 4;       // start with exactly a full deck
        public const int StarterCoins = 0;
        public const int TeamSize = 4;

        // --- ranked Elo (standard formula; bots are calibrated ground truth, so their rating is a
        //     fixed anchor and only the player's rating moves) ---------------------------------
        public const int StartElo = 1000;
        public const int EloKNew = 40;              // K-factor while provisional (fast convergence)
        public const int EloKEstablished = 24;      // K-factor after the provisional period
        public const int EloProvisionalGames = 30;  // games before the rating is "established"

        // --- move progression (slow burn: ~4 wins to unlock one move) -------------------------
        public const int BasicMoveCount = 4;        // weakest moves you spawn with
        public const int MoveUnlockCost = 10;       // points to unlock a move
        public const int WinMovePts = 3;            // progress per win…
        public const int LossMovePts = 1;           // …and per loss
        public const int MaxProgressMoves = 3;      // up to this many moves gain progress per game

        // Rarity tiers, by base-stat total. Index 0..3 = Common/Rare/Epic/Legendary.
        public static readonly string[] RarityNames = { "Common", "Rare", "Epic", "Legendary" };
        static readonly int[] RarityWeights = { 60, 27, 10, 3 };

        static PlayerProfile _profile;
        static List<string> _pool;                 // all summonable species ids
        static Sim.Data.Dex _dex;                  // for base stats (rarity) + names
        static Sim.Data.RandbatsDex _randbats;     // per-species move pools
        static Dictionary<int, List<string>> _byTier;

        static string SavePath => System.IO.Path.Combine(Application.persistentDataPath, "profile.json");

        public static PlayerProfile Profile => _profile ??= Load();
        public static IReadOnlyList<string> Pool => _pool ??= LoadPool();
        public static Sim.Data.Dex Dex => _dex ??= SafeLoadDex();
        public static Sim.Data.RandbatsDex Randbats => _randbats ??= SafeLoadRandbats();

        static Sim.Data.Dex SafeLoadDex() { try { return DexLoader.LoadFromStreamingAssets(); } catch { return null; } }
        static Sim.Data.RandbatsDex SafeLoadRandbats() { try { return RandbatsLoader.LoadFromStreamingAssets(); } catch { return null; } }

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
            if (p.elo <= 0) p.elo = StartElo;                       // migrate older saves
            if (string.IsNullOrEmpty(p.username)) p.username = "You";
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

        // ---- ranked matchmaking + results ---------------------------------------------------

        /// <summary>The opponent the player is matched against this session: a flavor name + an Elo
        /// near the player's, generated when matchmaking starts and reused on the result screen.</summary>
        public static (string name, int elo) CurrentOpponent { get; private set; }

        static readonly string[] OppNames = {
            "Volt", "Nova", "Riptide", "Ember", "Quartz", "Specter", "Talon", "Bramble",
            "Cinder", "Frost", "Onyx", "Zephyr", "Hex", "Pyre", "Wisp", "Gale",
        };

        /// <summary>Pick a fresh opponent near the player's rating. Call when the search begins.</summary>
        public static (string name, int elo) StartMatchmaking()
        {
            var rng = new System.Random();
            int elo = System.Math.Max(100, Profile.elo + rng.Next(-90, 91));
            string name = OppNames[rng.Next(OppNames.Length)] + rng.Next(10, 100);
            CurrentOpponent = (name, elo);
            return CurrentOpponent;
        }

        /// <summary>Outcome of a finished battle: the standard-Elo rating change plus the coin reward.</summary>
        public struct MatchResult
        {
            public bool won;
            public int coins;
            public int eloDelta, newElo;       // player's rating change
            public string oppName;
            public int oppElo;                 // bot's calibrated rating (fixed ground truth — does not move)
            public double expected;            // pre-match win probability for the player (for display/debug)
        }

        public static MatchResult? LastResult { get; private set; }

        /// <summary>Standard Elo expected score: the player's win probability vs a given opponent rating.</summary>
        public static double ExpectedScore(int playerElo, int oppElo) =>
            1.0 / (1.0 + System.Math.Pow(10.0, (oppElo - playerElo) / 400.0));

        static int KFactor(PlayerProfile p) =>
            p.gamesPlayed < EloProvisionalGames ? EloKNew : EloKEstablished;

        /// <summary>Resolve a finished battle. Rating uses the standard Elo update against the bot's
        /// calibrated (ground-truth, fixed) rating; coins are a flat win/loss reward.</summary>
        public static MatchResult ResolveMatch(bool won)
        {
            var p = Profile;
            var opp = CurrentOpponent.elo > 0 ? CurrentOpponent : StartMatchmaking();

            // Standard Elo: R' = R + K*(S - E). Only the player moves; the bot is the fixed anchor.
            double expected = ExpectedScore(p.elo, opp.elo);
            double score = won ? 1.0 : 0.0;
            int eloDelta = (int)System.Math.Round(KFactor(p) * (score - expected));
            int coins = won ? WinReward : LossReward;

            p.elo = System.Math.Max(0, p.elo + eloDelta);
            p.coins += coins;
            p.gamesPlayed++;
            Save();

            var res = new MatchResult
            {
                won = won, coins = coins, eloDelta = eloDelta, newElo = p.elo,
                oppName = opp.name, oppElo = opp.elo, expected = expected,
            };
            LastResult = res;
            return res;
        }

        // ---- move progression -----------------------------------------------------------------

        /// <summary>Every move this species can ever learn (union of its randbats set pools).</summary>
        public static List<string> MovePool(string species)
        {
            var pool = new SortedSet<string>();
            if (Randbats != null && Randbats.Species.TryGetValue(species, out var rb))
                foreach (var set in rb.Sets)
                    foreach (var mv in set.MovepoolIds)
                        if (Dex != null && Dex.Moves.ContainsKey(mv)) pool.Add(mv);
            return pool.ToList();
        }

        // Status moves count as mid-strength utility so the basic set is weak attacks + a little
        // utility, with the heavy hitters left to unlock.
        static int MoveScore(string id) =>
            Dex.Moves.TryGetValue(id, out var m) ? (m.BasePower <= 0 ? 50 : m.BasePower) : 50;

        /// <summary>The starter moves: the 2 weakest ATTACKS (a mon must be able to deal damage),
        /// then the weakest remaining moves to fill <see cref="BasicMoveCount"/>.</summary>
        public static List<string> BasicMoves(string species)
        {
            var pool = MovePool(species);
            bool IsAttack(string id) => Dex.Moves.TryGetValue(id, out var m) && m.BasePower > 0;
            var basics = pool.Where(IsAttack).OrderBy(MoveScore).ThenBy(id => id).Take(2).ToList();
            basics.AddRange(pool.Where(id => !basics.Contains(id))
                                .OrderBy(MoveScore).ThenBy(id => id)
                                .Take(BasicMoveCount - basics.Count));
            return basics;
        }

        /// <summary>This species' unlock state, created (with basics unlocked+equipped) on first use.</summary>
        public static MonMoves GetMonMoves(string species)
        {
            var p = Profile;
            var mm = p.monMoves.FirstOrDefault(m => m.species == species);
            if (mm == null)
            {
                var basics = BasicMoves(species);
                mm = new MonMoves { species = species, unlocked = new List<string>(basics), equipped = new List<string>(basics) };
                p.monMoves.Add(mm);
                Save();
            }
            return mm;
        }

        /// <summary>Equip/unequip an unlocked move (max 4 equipped, min 1). True if changed.</summary>
        public static bool ToggleEquip(string species, string moveId)
        {
            var mm = GetMonMoves(species);
            if (mm.equipped.Contains(moveId))
            {
                if (mm.equipped.Count <= 1) return false;        // never strip the last move
                mm.equipped.Remove(moveId);
            }
            else
            {
                if (!mm.unlocked.Contains(moveId) || mm.equipped.Count >= 4) return false;
                mm.equipped.Add(moveId);
            }
            Save();
            return true;
        }

        /// <summary>The equipped moves resolved to MoveData, for building the battle team.</summary>
        public static List<Sim.Data.MoveData> EquippedMoveDatas(string species)
        {
            var outList = new List<Sim.Data.MoveData>();
            if (Dex == null) return outList;
            foreach (var id in GetMonMoves(species).equipped)
                if (Dex.Moves.TryGetValue(id, out var m)) outList.Add(m);
            return outList;
        }

        /// <summary>One move's progress gain from a battle, for the result screen.</summary>
        public struct MoveGain
        {
            public string species, moveId;
            public int pts, total;
            public bool justUnlocked;
        }

        /// <summary>After a battle: up to <see cref="MaxProgressMoves"/> random locked moves across
        /// the battle team gain points (win &gt; loss); at <see cref="MoveUnlockCost"/> they unlock.</summary>
        public static List<MoveGain> AwardMoveProgress(bool won, IEnumerable<string> teamSpecies)
        {
            var gains = new List<MoveGain>();
            var rng = new System.Random();
            int pts = won ? WinMovePts : LossMovePts;

            // All still-locked moves across the team.
            var locked = new List<(string species, string move)>();
            foreach (var sp in teamSpecies.Distinct())
            {
                var mm = GetMonMoves(sp);
                foreach (var mv in MovePool(sp))
                    if (!mm.unlocked.Contains(mv)) locked.Add((sp, mv));
            }

            foreach (var (sp, mv) in locked.OrderBy(_ => rng.Next()).Take(MaxProgressMoves))
            {
                var mm = GetMonMoves(sp);
                int i = mm.progressIds.IndexOf(mv);
                if (i < 0) { mm.progressIds.Add(mv); mm.progressPts.Add(0); i = mm.progressIds.Count - 1; }
                mm.progressPts[i] += pts;
                int total = mm.progressPts[i];
                bool unlockedNow = total >= MoveUnlockCost;
                if (unlockedNow)
                {
                    mm.unlocked.Add(mv);
                    mm.progressIds.RemoveAt(i); mm.progressPts.RemoveAt(i);
                }
                gains.Add(new MoveGain { species = sp, moveId = mv, pts = pts, total = total, justUnlocked = unlockedNow });
            }
            Save();
            return gains;
        }

        /// <summary>Pretty move name for UI ("thunderbolt" → "Thunderbolt").</summary>
        public static string MoveName(string id) =>
            Dex != null && Dex.Moves.TryGetValue(id, out var m) ? m.Name : id;

        // --- leveling (Showdown-style: level only scales stats) -------------------------------
        // Mons start StartLevelOffset below their randbats level and earn XP per battle, per mon.
        // The randbats level is the CAP — it's the point the format balanced each species at.
        public const int StartLevelOffset = 10;
        public const int XpPerLevel = 30;
        public const int XpBattled = 10;            // sent out at least once
        public const int XpBenched = 3;             // on the team but never sent out
        public const int XpWinBonus = 8;            // each team mon, on a win
        public const int XpSurviveBonus = 4;        // battled and didn't faint

        /// <summary>The species' randbats level (its balance point / level cap).</summary>
        public static int LevelCap(string species) =>
            Randbats != null && Randbats.Species.TryGetValue(species, out var e) && e.Level > 0 ? e.Level : 80;

        /// <summary>The mon's current level: cap − 10, grown back up by earned levels.</summary>
        public static int CurrentLevel(string species)
        {
            int cap = LevelCap(species);
            return Mathf.Min(cap, cap - StartLevelOffset + GetMonMoves(species).levelsGained);
        }

        public struct XpGain
        {
            public string species;
            public int xp;                  // xp awarded this battle
            public int oldLevel, newLevel;  // level before/after (differ on level-up)
            public float fracFrom, fracTo;  // xp-bar fill within the current level
        }

        /// <summary>Award per-mon XP for a finished battle and apply level-ups. Capped mons are skipped.</summary>
        public static List<XpGain> AwardXp(bool won, IEnumerable<(string species, bool battled, bool survived)> team)
        {
            var gains = new List<XpGain>();
            foreach (var (sp, battled, survived) in team)
            {
                int cap = LevelCap(sp);
                var mm = GetMonMoves(sp);
                int oldLevel = CurrentLevel(sp);
                if (oldLevel >= cap) continue; // maxed — no XP to earn

                int xp = (battled ? XpBattled : XpBenched)
                       + (won ? XpWinBonus : 0)
                       + (battled && survived ? XpSurviveBonus : 0);
                int oldXp = mm.xp;
                mm.xp += xp;
                while (mm.xp >= XpPerLevel && CurrentLevel(sp) < cap)
                {
                    mm.xp -= XpPerLevel;
                    mm.levelsGained++;
                }
                int newLevel = CurrentLevel(sp);
                if (newLevel >= cap) mm.xp = 0; // hit the cap — discard leftover progress

                gains.Add(new XpGain
                {
                    species = sp, xp = xp, oldLevel = oldLevel, newLevel = newLevel,
                    fracFrom = (float)oldXp / XpPerLevel,
                    fracTo = newLevel > oldLevel ? 1f : (float)mm.xp / XpPerLevel,
                });
            }
            Save();
            return gains;
        }
    }
}
