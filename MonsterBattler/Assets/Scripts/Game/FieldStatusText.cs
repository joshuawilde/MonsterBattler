using System.Collections.Generic;
using MonsterBattler.Sim;

namespace MonsterBattler.Game
{
    /// <summary>
    /// Formats the persistent battle conditions Showdown shows as on-field indicators: weather /
    /// terrain / Trick Room (field-wide) and hazards / screens / Tailwind (per side). Pure strings.
    /// </summary>
    public static class FieldStatusText
    {
        /// <summary>Field-wide conditions (weather, terrain, Trick Room). Empty if none.</summary>
        public static string Field(Battle battle)
        {
            var f = battle.Field;
            var parts = new List<string>();
            if (f.Weather != Weather.None) parts.Add($"{WeatherName(f.Weather)} ({f.WeatherTurnsLeft})");
            if (f.Terrain != Terrain.None) parts.Add($"{f.Terrain} Terrain ({f.TerrainTurnsLeft})");
            if (f.Conditions.TryGetValue("trickroom", out var tr)) parts.Add($"Trick Room ({tr.TurnsLeft})");
            return string.Join("    ", parts);
        }

        /// <summary>One side's hazards / screens / Tailwind etc. Empty if none.</summary>
        public static string Side(Side side)
        {
            var parts = new List<string>();
            foreach (var kv in side.Conditions) parts.Add(Label(kv.Key, kv.Value));
            return string.Join(", ", parts);
        }

        static string Label(string id, SideCondition c) => id switch
        {
            "stealthrock" => "Rocks",
            "spikes" => c.Layers > 1 ? $"Spikes×{c.Layers}" : "Spikes",
            "toxicspikes" => c.Layers > 1 ? $"T-Spikes×{c.Layers}" : "T-Spikes",
            "stickyweb" => "Web",
            "reflect" => "Reflect",
            "lightscreen" => "Light Screen",
            "auroraveil" => "Aurora Veil",
            "tailwind" => $"Tailwind ({c.TurnsLeft})",
            "safeguard" => "Safeguard",
            "mist" => "Mist",
            _ => id,
        };

        static string WeatherName(Weather w) => w switch
        {
            Weather.Sun => "Sun", Weather.Rain => "Rain", Weather.Sandstorm => "Sandstorm",
            Weather.Snow => "Snow", Weather.HarshSun => "Harsh Sun", Weather.HeavyRain => "Heavy Rain",
            Weather.StrongWinds => "Strong Winds", _ => w.ToString(),
        };
    }
}
