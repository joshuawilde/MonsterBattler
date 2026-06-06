using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using MonsterBattler.Sim;
using MonsterBattler.Sim.Data;

namespace MonsterBattler.Sim.Tests
{
    /// <summary>
    /// Loads the real StreamingAssets dex JSON into Sim data objects, mirroring the Unity-side
    /// DexLoader/RandbatsLoader field-for-field but with System.Text.Json (no Unity, no Newtonsoft).
    /// Cached so the whole suite parses the data once.
    /// </summary>
    public static class TestData
    {
        static Dex _dex;
        static RandbatsDex _randbats;

        public static Dex Dex => _dex ??= LoadDex();
        public static RandbatsDex Randbats => _randbats ??= LoadRandbats();

        static string DexDir([CallerFilePath] string thisFile = "")
        {
            // thisFile = .../tests/MonsterBattler.Sim.Tests/TestData.cs
            var testsDir = Path.GetDirectoryName(thisFile);
            var repoRoot = Path.GetFullPath(Path.Combine(testsDir, "..", ".."));
            return Path.Combine(repoRoot, "MonsterBattler", "Assets", "StreamingAssets", "dex");
        }

        static JsonElement Read(string file)
        {
            var path = Path.Combine(DexDir(), file);
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.Clone();
        }

        static int Int(JsonElement o, string k, int def = 0)
            => o.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : def;
        static bool Bool(JsonElement o, string k)
            => o.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.True;
        static string Str(JsonElement o, string k, string def = null)
            => o.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : def;
        static bool Flag(JsonElement flags, string k)
            => flags.ValueKind == JsonValueKind.Object && flags.TryGetProperty(k, out var v)
               && v.ValueKind == JsonValueKind.Number && v.GetInt32() == 1;

        static MonType Type(string s)
            => string.IsNullOrEmpty(s) ? MonType.None
               : Enum.TryParse<MonType>(s, true, out var t) ? t : MonType.None;

        static Dex LoadDex()
        {
            var dex = new Dex();

            foreach (var (id, o) in Read("species.json").EnumerateObject().Select())
            {
                var types = new List<string>();
                if (o.TryGetProperty("types", out var ta))
                    foreach (var t in ta.EnumerateArray()) types.Add(t.GetString());
                var stats = o.GetProperty("baseStats");
                var abilities = new List<string>();
                if (o.TryGetProperty("abilities", out var aa))
                    foreach (var a in aa.EnumerateArray()) abilities.Add(a.GetString());
                dex.Species[id] = new SpeciesData
                {
                    Id = id,
                    Name = Str(o, "name", id),
                    Type1 = types.Count > 0 ? Type(types[0]) : MonType.None,
                    Type2 = types.Count > 1 ? Type(types[1]) : MonType.None,
                    BaseStats = new BaseStats
                    {
                        HP = Int(stats, "hp"), Atk = Int(stats, "atk"), Def = Int(stats, "def"),
                        SpA = Int(stats, "spa"), SpD = Int(stats, "spd"), Spe = Int(stats, "spe"),
                    },
                    AbilityIds = abilities,
                };
            }

            foreach (var (id, o) in Read("moves.json").EnumerateObject().Select())
            {
                var flags = o.TryGetProperty("flags", out var f) ? f : default;
                Enum.TryParse<MoveCategory>(Str(o, "category", "Status"), true, out var cat);
                Enum.TryParse<MoveTarget>(Str(o, "target", "Normal"), true, out var tgt);
                dex.Moves[id] = new MoveData
                {
                    Id = id, Name = Str(o, "name", id), Type = Type(Str(o, "type")), Category = cat,
                    BasePower = Int(o, "basePower"), Accuracy = Int(o, "accuracy"), Pp = Int(o, "pp"),
                    Priority = Int(o, "priority"), CritRatio = Int(o, "critRatio"),
                    RecoilNum = Int(o, "recoilNum"), RecoilDen = Int(o, "recoilDen"),
                    DrainNum = Int(o, "drainNum"), DrainDen = Int(o, "drainDen"),
                    SelfKO = Bool(o, "selfKO"), PivotsOut = Bool(o, "pivotsOut"),
                    MultihitMin = Int(o, "multihitMin"), MultihitMax = Int(o, "multihitMax"),
                    FlinchChance = Int(o, "flinchChance"), TwoTurn = Bool(o, "twoTurn"), Target = tgt,
                    EffectId = Str(o, "effectId"),
                    Secondaries = ParseSecondaries(o),
                    SelfBoosts = o.TryGetProperty("selfBoosts", out var sb) ? ParseBoosts(sb) : null,
                    Contact = Flag(flags, "contact"), Protect = Flag(flags, "protect"),
                    Sound = Flag(flags, "sound"), Punch = Flag(flags, "punch"), Bite = Flag(flags, "bite"),
                    Slicing = Flag(flags, "slicing"), Wind = Flag(flags, "wind"), Bullet = Flag(flags, "bullet"),
                };
            }

            foreach (var (id, o) in Read("abilities.json").EnumerateObject().Select())
                dex.Abilities[id] = new AbilityData { Id = id, Name = Str(o, "name", id), EffectId = Str(o, "effectId") };

            foreach (var (id, o) in Read("items.json").EnumerateObject().Select())
                dex.Items[id] = new ItemData
                {
                    Id = id, Name = Str(o, "name", id), EffectId = Str(o, "effectId"),
                    IsBerry = Bool(o, "isBerry"), ConsumedOnUse = Bool(o, "consumedOnUse"),
                };

            return dex;
        }

        static MoveSecondary[] ParseSecondaries(JsonElement move)
        {
            if (!move.TryGetProperty("secondaries", out var arr) || arr.ValueKind != JsonValueKind.Array)
                return null;
            var list = new List<MoveSecondary>();
            foreach (var e in arr.EnumerateArray())
                list.Add(new MoveSecondary
                {
                    Chance = Int(e, "chance"),
                    Status = Str(e, "status"),
                    Volatile = Str(e, "volatile"),
                    TargetBoosts = e.TryGetProperty("boosts", out var b) ? ParseBoosts(b) : null,
                    SelfBoosts = e.TryGetProperty("self", out var s) ? ParseBoosts(s) : null,
                });
            return list.Count > 0 ? list.ToArray() : null;
        }

        static StatChange[] ParseBoosts(JsonElement arr)
        {
            if (arr.ValueKind != JsonValueKind.Array) return null;
            var list = new List<StatChange>();
            foreach (var e in arr.EnumerateArray())
                if (Enum.TryParse<Stat>(Str(e, "stat"), true, out var st))
                    list.Add(new StatChange { Stat = st, Delta = Int(e, "delta") });
            return list.Count > 0 ? list.ToArray() : null;
        }

        static readonly Regex NonAlnum = new("[^a-z0-9]", RegexOptions.Compiled);
        static string ToId(string s) => s == null ? null : NonAlnum.Replace(s.ToLowerInvariant(), "");

        static RandbatsDex LoadRandbats()
        {
            var rb = new RandbatsDex();
            foreach (var (id, o) in Read("randbats.json").EnumerateObject().Select())
            {
                var entry = new RandbatsSpecies { Level = Int(o, "level") };
                if (o.TryGetProperty("sets", out var sets))
                    foreach (var s in sets.EnumerateArray())
                    {
                        var set = new RandbatsSet { Role = Str(s, "role") };
                        if (s.TryGetProperty("movepool", out var mp))
                            foreach (var m in mp.EnumerateArray()) set.MovepoolIds.Add(ToId(m.GetString()));
                        if (s.TryGetProperty("abilities", out var ab))
                            foreach (var a in ab.EnumerateArray()) set.AbilityIds.Add(ToId(a.GetString()));
                        if (s.TryGetProperty("teraTypes", out var tt))
                            foreach (var t in tt.EnumerateArray()) set.TeraTypes.Add(Type(t.GetString()));
                        entry.Sets.Add(set);
                    }
                rb.Species[id] = entry;
            }
            return rb;
        }

        // Small helper so EnumerateObject reads as (id, value) tuples.
        static IEnumerable<(string Name, JsonElement Value)> Select(this JsonElement.ObjectEnumerator e)
        {
            foreach (var p in e) yield return (p.Name, p.Value);
        }
    }
}
