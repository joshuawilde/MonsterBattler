using System.Collections.Generic;
using System.Text;
using MonsterBattler.Sim;

namespace MonsterBattler.Game
{
    /// <summary>
    /// Builds the Showdown-style info blurb for a Pokemon: types, stats, ability (+desc), item, the
    /// defensive type matchup (weaknesses / resistances / immunities), and each move with its
    /// description. Pure string work (no Unity), so it's unit-testable.
    /// </summary>
    public static class PokemonInfoText
    {
        public static string Build(Pokemon m)
        {
            if (m == null || m.Species == null) return "";
            var sb = new StringBuilder();

            string types = m.Species.Type2 != MonType.None
                ? $"{m.Species.Type1}/{m.Species.Type2}" : m.Species.Type1.ToString();
            if (m.IsTerastallized) types += $"  (Tera {m.TeraType})";
            sb.AppendLine($"{m.Species.Name}   {types}   Lv{m.Level}");

            sb.AppendLine($"HP {m.MaxStats[(int)Stat.HP]}  Atk {m.MaxStats[(int)Stat.Atk]}  " +
                          $"Def {m.MaxStats[(int)Stat.Def]}  SpA {m.MaxStats[(int)Stat.SpA]}  " +
                          $"SpD {m.MaxStats[(int)Stat.SpD]}  Spe {m.MaxStats[(int)Stat.Spe]}");

            if (m.Ability != null)
            {
                sb.Append($"Ability: {m.Ability.Name}");
                if (!string.IsNullOrEmpty(m.Ability.ShortDesc)) sb.Append($" — {m.Ability.ShortDesc}");
                sb.AppendLine();
            }
            sb.AppendLine($"Item: {(m.Item != null ? m.Item.Name : "None")}");

            // Defensive matchup — uses the Tera type when terastallized, like the live battle.
            var t1 = m.IsTerastallized ? m.TeraType : m.Species.Type1;
            var t2 = m.IsTerastallized ? MonType.None : m.Species.Type2;
            var weak = new List<string>();
            var resist = new List<string>();
            var immune = new List<string>();
            foreach (var e in TypeMatchup.Defensive(t1, t2))
            {
                if (e.Multiplier == 0f) immune.Add(e.Type.ToString());
                else if (e.Multiplier > 1f) weak.Add($"{e.Type} {Mult(e.Multiplier)}");
                else resist.Add($"{e.Type} {Mult(e.Multiplier)}");
            }
            sb.AppendLine($"Weak: {Join(weak)}");
            sb.AppendLine($"Resists: {Join(resist)}");
            sb.AppendLine($"Immune: {Join(immune)}");

            sb.AppendLine("Moves:");
            foreach (var slot in m.Moves)
            {
                var mv = slot.Move;
                string ty = mv.Type != MonType.None ? mv.Type.ToString() : "Status";
                string desc = string.IsNullOrEmpty(mv.ShortDesc) ? "" : $" — {mv.ShortDesc}";
                sb.AppendLine($"• {mv.Name} ({ty}, {slot.Pp}/{slot.MaxPp} PP){desc}");
            }
            return sb.ToString().TrimEnd();
        }

        static string Join(List<string> xs) => xs.Count > 0 ? string.Join(", ", xs) : "—";

        static string Mult(float m) => m switch
        {
            4f => "×4", 2f => "×2", 0.5f => "×½", 0.25f => "×¼", 0f => "×0",
            _ => $"×{m:0.##}",
        };
    }
}
