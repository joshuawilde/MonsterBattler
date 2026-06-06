using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using MonsterBattler.Sim;
using MonsterBattler.Sim.Data;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MonsterBattler.Game
{
    /// <summary>
    /// Loads the curated gen9 random-battle sets from <c>StreamingAssets/dex/randbats.json</c>
    /// (extracted from @pkmn/randoms by tools/dex-import) into a <see cref="RandbatsDex"/>.
    /// Movepool/ability display names are converted to ids; tera-type names to <see cref="MonType"/>.
    /// Lives in the Game asmdef because it does IO + Newtonsoft, keeping the Sim asmdef pure.
    /// </summary>
    public static class RandbatsLoader
    {
        public static RandbatsDex LoadFromStreamingAssets()
        {
            var path = Path.Combine(Application.streamingAssetsPath, "dex", "randbats.json");
            var rb = new RandbatsDex();
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[RandbatsLoader] Missing file: {path}");
                return rb;
            }

            var root = JObject.Parse(File.ReadAllText(path));
            foreach (var (speciesId, value) in root)
            {
                if (value is not JObject o) continue;
                var entry = new RandbatsSpecies { Level = (int?)o["level"] ?? 0 };
                if (o["sets"] is JArray sets)
                {
                    foreach (var s in sets)
                    {
                        if (s is not JObject so) continue;
                        var set = new RandbatsSet { Role = (string)so["role"] };
                        foreach (var m in so["movepool"] as JArray ?? new JArray())
                            set.MovepoolIds.Add(ToId((string)m));
                        foreach (var a in so["abilities"] as JArray ?? new JArray())
                            set.AbilityIds.Add(ToId((string)a));
                        foreach (var t in so["teraTypes"] as JArray ?? new JArray())
                            set.TeraTypes.Add(ParseType((string)t));
                        entry.Sets.Add(set);
                    }
                }
                rb.Species[speciesId] = entry;
            }
            return rb;
        }

        static readonly Regex NonAlnum = new("[^a-z0-9]", RegexOptions.Compiled);
        static string ToId(string s) => s == null ? null : NonAlnum.Replace(s.ToLowerInvariant(), "");

        static MonType ParseType(string s)
            => Enum.TryParse<MonType>(s, ignoreCase: true, out var t) ? t : MonType.None;
    }
}
