using System;
using System.Collections.Generic;
using System.IO;
using MonsterBattler.Sim;
using MonsterBattler.Sim.Data;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MonsterBattler.Game
{
    /// <summary>
    /// Reads JSON dex files from <c>Application.streamingAssetsPath/dex/</c> and populates a
    /// <see cref="Dex"/>. Format is intentionally close to Pokemon Showdown's so its data can be
    /// converted mechanically later (the TS-to-JSON conversion is a separate step we defer).
    ///
    /// Lives in the Game asmdef because it touches Unity (StreamingAssets) and Newtonsoft —
    /// the Sim asmdef stays pure C# with no IO.
    /// </summary>
    public static class DexLoader
    {
        public static Dex LoadFromStreamingAssets()
        {
            var root = Path.Combine(Application.streamingAssetsPath, "dex");
            var dex = new Dex();
            LoadSpecies(dex, Path.Combine(root, "species.json"));
            LoadMoves(dex, Path.Combine(root, "moves.json"));
            LoadAbilities(dex, Path.Combine(root, "abilities.json"));
            LoadItems(dex, Path.Combine(root, "items.json"));
            return dex;
        }

        static JObject ReadObj(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[DexLoader] Missing file: {path}");
                return new JObject();
            }
            return JObject.Parse(File.ReadAllText(path));
        }

        static void LoadSpecies(Dex dex, string path)
        {
            var root = ReadObj(path);
            foreach (var (id, value) in root)
            {
                if (value is not JObject o) continue;
                var types = (o["types"] as JArray)?.ToObject<List<string>>() ?? new List<string>();
                var stats = o["baseStats"] as JObject ?? new JObject();
                var sp = new SpeciesData
                {
                    Id = id,
                    Name = (string)o["name"] ?? id,
                    Type1 = types.Count > 0 ? ParseType(types[0]) : MonType.None,
                    Type2 = types.Count > 1 ? ParseType(types[1]) : MonType.None,
                    BaseStats = new BaseStats
                    {
                        HP  = (int?)stats["hp"]  ?? 0,
                        Atk = (int?)stats["atk"] ?? 0,
                        Def = (int?)stats["def"] ?? 0,
                        SpA = (int?)stats["spa"] ?? 0,
                        SpD = (int?)stats["spd"] ?? 0,
                        Spe = (int?)stats["spe"] ?? 0,
                    },
                    AbilityIds = (o["abilities"] as JArray)?.ToObject<List<string>>() ?? new List<string>(),
                };
                dex.Species[id] = sp;
            }
        }

        static void LoadMoves(Dex dex, string path)
        {
            var root = ReadObj(path);
            foreach (var (id, value) in root)
            {
                if (value is not JObject o) continue;
                var flags = o["flags"] as JObject ?? new JObject();
                var move = new MoveData
                {
                    Id = id,
                    Name = (string)o["name"] ?? id,
                    Type = ParseType((string)o["type"]),
                    Category = ParseCategory((string)o["category"]),
                    BasePower = (int?)o["basePower"] ?? 0,
                    Accuracy = (int?)o["accuracy"] ?? 0,
                    Pp = (int?)o["pp"] ?? 0,
                    Priority = (int?)o["priority"] ?? 0,
                    CritRatio = (int?)o["critRatio"] ?? 0,
                    RecoilNum = (int?)o["recoilNum"] ?? 0,
                    RecoilDen = (int?)o["recoilDen"] ?? 0,
                    DrainNum  = (int?)o["drainNum"]  ?? 0,
                    DrainDen  = (int?)o["drainDen"]  ?? 0,
                    SelfKO    = (bool?)o["selfKO"]   ?? false,
                    PivotsOut = (bool?)o["pivotsOut"] ?? false,
                    MultihitMin = (int?)o["multihitMin"] ?? 0,
                    MultihitMax = (int?)o["multihitMax"] ?? 0,
                    FlinchChance = (int?)o["flinchChance"] ?? 0,
                    TwoTurn = (bool?)o["twoTurn"] ?? false,
                    Target = ParseTarget((string)o["target"]),
                    EffectId = (string)o["effectId"],
                    Secondaries = ParseSecondaries(o["secondaries"] as JArray),
                    SelfBoosts = ParseBoosts(o["selfBoosts"] as JArray),
                    ShortDesc = (string)o["desc"],
                    Contact = (int?)flags["contact"] == 1,
                    Protect = (int?)flags["protect"] == 1,
                    Sound   = (int?)flags["sound"]   == 1,
                    Punch   = (int?)flags["punch"]   == 1,
                    Bite    = (int?)flags["bite"]    == 1,
                    Slicing = (int?)flags["slicing"] == 1,
                    Wind    = (int?)flags["wind"]    == 1,
                    Bullet  = (int?)flags["bullet"]  == 1,
                };
                dex.Moves[id] = move;
            }
        }

        static void LoadAbilities(Dex dex, string path)
        {
            var root = ReadObj(path);
            foreach (var (id, value) in root)
            {
                if (value is not JObject o) continue;
                dex.Abilities[id] = new AbilityData
                {
                    Id = id,
                    Name = (string)o["name"] ?? id,
                    EffectId = (string)o["effectId"],
                    ShortDesc = (string)o["desc"],
                };
            }
        }

        static void LoadItems(Dex dex, string path)
        {
            var root = ReadObj(path);
            foreach (var (id, value) in root)
            {
                if (value is not JObject o) continue;
                dex.Items[id] = new ItemData
                {
                    Id = id,
                    Name = (string)o["name"] ?? id,
                    EffectId = (string)o["effectId"],
                    IsBerry = (bool?)o["isBerry"] ?? false,
                    ConsumedOnUse = (bool?)o["consumedOnUse"] ?? false,
                };
            }
        }

        static MoveSecondary[] ParseSecondaries(JArray arr)
        {
            if (arr == null) return null;
            var list = new List<MoveSecondary>();
            foreach (var e in arr)
            {
                if (e is not JObject o) continue;
                list.Add(new MoveSecondary
                {
                    Chance = (int?)o["chance"] ?? 0,
                    Status = (string)o["status"],
                    Volatile = (string)o["volatile"],
                    TargetBoosts = ParseBoosts(o["boosts"] as JArray),
                    SelfBoosts = ParseBoosts(o["self"] as JArray),
                });
            }
            return list.Count > 0 ? list.ToArray() : null;
        }

        static StatChange[] ParseBoosts(JArray arr)
        {
            if (arr == null) return null;
            var list = new List<StatChange>();
            foreach (var e in arr)
            {
                if (e is not JObject o) continue;
                if (Enum.TryParse<Stat>((string)o["stat"], ignoreCase: true, out var st))
                    list.Add(new StatChange { Stat = st, Delta = (int?)o["delta"] ?? 0 });
            }
            return list.Count > 0 ? list.ToArray() : null;
        }

        static MonType ParseType(string s)
        {
            if (string.IsNullOrEmpty(s)) return MonType.None;
            return Enum.TryParse<MonType>(s, ignoreCase: true, out var t) ? t : MonType.None;
        }

        static MoveCategory ParseCategory(string s)
        {
            if (string.IsNullOrEmpty(s)) return MoveCategory.Status;
            return Enum.TryParse<MoveCategory>(s, ignoreCase: true, out var c) ? c : MoveCategory.Status;
        }

        static MoveTarget ParseTarget(string s)
        {
            if (string.IsNullOrEmpty(s)) return MoveTarget.Normal;
            // Accept PS-style identifiers like "self", "allAdjacentFoes", "normal".
            return Enum.TryParse<MoveTarget>(s, ignoreCase: true, out var t) ? t : MoveTarget.Normal;
        }
    }
}
