using System.Collections.Generic;

namespace MonsterBattler.Game
{
    /// <summary>
    /// Translates the sim's Pokemon-Showdown-style protocol lines (e.g. <c>|-damage|Pikachu|10/100|[from] brn</c>)
    /// into human-readable battle-log text (<c>"Pikachu was hurt by its burn!"</c>). Returns null for
    /// lines that shouldn't surface as text (silent updates the HP bars already convey).
    /// Pure string work — no Unity dependency, so it's trivially testable.
    /// </summary>
    public static class BattleLogFormatter
    {
        public static string Format(string line)
        {
            if (string.IsNullOrEmpty(line)) return null;
            var p = line.Split('|'); // p[0]="" , p[1]=tag, p[2..]=args
            if (p.Length < 2) return null;
            string Arg(int i) => i < p.Length ? p[i] : "";
            string from = FromSource(p);

            switch (p[1])
            {
                case "turn":          return $"— Turn {Arg(2)} —";
                case "move":          return $"{Arg(2)} used {Arg(3)}!";
                case "switch":        return $"{Arg(2)} was sent out!";
                case "faint":         return $"{Arg(2)} fainted!";
                case "-crit":         return "A critical hit!";
                case "-miss":         return $"{Arg(2)}'s attack missed!";
                case "-fail":         return $"But it failed!";
                case "-immune":       return $"It had no effect on {Arg(2)}…";
                case "-hitcount":     return $"Hit {Arg(3)} time(s)!";
                case "-terastallize": return $"{Arg(2)} Terastallized into {Arg(3)}!";
                case "-formechange":  return $"{Arg(2)} transformed into {Arg(3)}!";
                case "-prepare":      return $"{Arg(2)} is readying {Arg(3)}!";

                case "-status":       return $"{Arg(2)} {StatusVerb(Arg(3))}!";
                case "-curestatus":   return Arg(3) == "slp" ? $"{Arg(2)} woke up!" : $"{Arg(2)}'s status was cured!";

                case "-boost":        return $"{Arg(2)}'s {StatName(Arg(3))} {RoseVerb(Arg(4))}!";
                case "-unboost":      return $"{Arg(2)}'s {StatName(Arg(3))} {FellVerb(Arg(4))}!";
                case "-setboost":     return $"{Arg(2)} maxed its {StatName(Arg(3))}!";

                case "-weather":      return Arg(2) == "none" ? "The weather cleared." : $"{WeatherName(Arg(2))}!";
                case "-fieldstart":   return $"{StripMove(Arg(2))} took effect!";
                case "-fieldend":     return $"{StripMove(Arg(2))} wore off.";
                case "-sidestart":    return $"{Arg(3)} was set up.";
                case "-sideend":      return $"{Arg(3)} disappeared.";

                case "-heal":         return from != null ? $"{Arg(2)} restored HP ({from})." : $"{Arg(2)} restored health.";
                case "-damage":       return from != null ? $"{Arg(2)} was hurt by {from}!" : null; // direct damage shown by HP bar
                case "-sethp":        return null;

                case "-item":         return $"{Arg(2)} obtained {Arg(3)}!";
                case "-enditem":      return from != null ? $"{Arg(2)}'s {Arg(3)} was {from}!" : $"{Arg(2)}'s {Arg(3)} was used up!";
                case "-ability":      return $"{Arg(2)}'s {Arg(3)}!";

                case "-start":        return StartText(Arg(2), Arg(3));
                case "-end":          return $"{Arg(2)}'s {Readable(Arg(3))} ended.";
                case "-activate":     return $"{Arg(2)}'s {Readable(StripPrefix(Arg(3)))} activated!";

                case "cant":          return $"{Arg(2)} {CantReason(Arg(3))}!";
                default:              return null;
            }
        }

        // "[from] brn" / "[from] item: Life Orb" / "[from] move: Knock Off" → readable source, else null
        static string FromSource(string[] p)
        {
            for (int i = 2; i < p.Length; i++)
            {
                if (!p[i].StartsWith("[from]")) continue;
                var s = p[i].Substring("[from]".Length).Trim();
                if (s.Contains("Knock Off")) return "knocked off";
                if (s.StartsWith("item:")) return s.Substring(5).Trim();
                if (s.StartsWith("ability:")) return s.Substring(8).Trim();
                if (s.StartsWith("move:")) return s.Substring(5).Trim();
                return ResidualSource(s);
            }
            return null;
        }

        static string ResidualSource(string s) => s switch
        {
            "brn" => "its burn", "psn" => "poison", "tox" => "poison",
            "confusion" => "confusion", "Recoil" => "recoil",
            "partiallytrapped" => "the trap", "drain" => "draining",
            _ => s, // Stealth Rock, Spikes, Sandstorm, Leech Seed, Substitute, etc.
        };

        static string StatusVerb(string code) => code switch
        {
            "brn" => "was burned", "psn" => "was poisoned", "tox" => "was badly poisoned",
            "par" => "was paralyzed", "slp" => "fell asleep", "frz" => "was frozen solid",
            "fbt" => "got frostbite", _ => $"was afflicted ({code})",
        };

        static string CantReason(string code) => code switch
        {
            "flinch" => "flinched and couldn't move", "slp" => "is fast asleep",
            "par" => "is paralyzed and can't move", "frz" => "is frozen solid",
            "partiallytrapped" => "can't escape", "recharge" => "must recharge",
            _ => "can't move",
        };

        static string StartText(string mon, string what) => Readable(what) switch
        {
            "confusion" => $"{mon} became confused!",
            "leechseed" => $"{mon} was seeded!",
            "substitute" => $"{mon} put up a substitute!",
            "Encore" => $"{mon} got an encore!",
            _ => $"{mon}'s {Readable(StripPrefix(what))} began!",
        };

        static string StatName(string s) => s switch
        {
            "atk" => "Attack", "def" => "Defense", "spa" => "Sp. Atk", "spd" => "Sp. Def",
            "spe" => "Speed", "accuracy" => "accuracy", "evasion" => "evasiveness", _ => s,
        };

        static string RoseVerb(string mag) => mag switch { "2" => "rose sharply", "3" => "rose drastically", _ => "rose" };
        static string FellVerb(string mag) => mag switch { "2" => "harshly fell", "3" => "severely fell", _ => "fell" };

        static string WeatherName(string w) => w switch
        {
            "Sun" => "The sunlight turned harsh", "Rain" => "It started to rain",
            "Sandstorm" => "A sandstorm kicked up", "Snow" => "It started to snow",
            _ => $"{w} set in",
        };

        static string StripMove(string s) => s.StartsWith("move:") ? s.Substring(5).Trim() : s;
        static string StripPrefix(string s)
        {
            int i = s.IndexOf(": ");
            return i >= 0 ? s.Substring(i + 2) : s;
        }
        static string Readable(string s) => StripMove(s);
    }
}
