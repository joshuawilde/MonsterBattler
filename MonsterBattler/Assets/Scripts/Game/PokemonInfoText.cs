using System.Collections.Generic;
using System.Text;
using MonsterBattler.Sim;

namespace MonsterBattler.Game
{
    /// <summary>
    /// Builds the text portions of the info panel as TMP rich text. Types and the defensive matchup
    /// are NOT here — those are real <see cref="UI.TypeBadge"/> chips placed by InfoPanel. Pure
    /// string work (no Unity), so it's unit-testable.
    /// </summary>
    public static class PokemonInfoText
    {
        /// <summary>Name + level (+ Tera), as a header line.</summary>
        public static string HeaderText(Pokemon m)
        {
            if (m == null || m.Species == null) return "";
            var sb = new StringBuilder();
            sb.Append($"<size=125%><b>{m.Species.Name}</b></size>   <b>L{m.Level}</b>");
            if (m.IsTerastallized) sb.Append($"   <i>Tera {m.TeraType}</i>");
            return sb.ToString();
        }

        /// <summary>HP% then the stat line (shown above the effectiveness rows).</summary>
        public static string TopBodyText(Pokemon m)
        {
            if (m == null || m.Species == null) return "";
            var sb = new StringBuilder();
            sb.AppendLine($"<b>HP:</b> {Pct(m)}%");
            sb.Append($"<b>HP</b> {m.MaxStats[(int)Stat.HP]}  {StatCell(m, "Atk", Stat.Atk)}  {StatCell(m, "Def", Stat.Def)}  " +
                      $"{StatCell(m, "SpA", Stat.SpA)}  {StatCell(m, "SpD", Stat.SpD)}  {StatCell(m, "Spe", Stat.Spe)}");
            return sb.ToString();
        }

        /// <summary>Ability + item + moves (shown below the effectiveness rows).</summary>
        public static string BottomBodyText(Pokemon m)
        {
            if (m == null || m.Species == null) return "";
            var sb = new StringBuilder();

            if (m.Ability != null)
            {
                sb.Append($"<b>Ability:</b> {m.Ability.Name}");
                if (!string.IsNullOrEmpty(m.Ability.ShortDesc)) sb.Append($" <size=85%>— {m.Ability.ShortDesc}</size>");
                sb.AppendLine();
            }
            if (m.Item != null)
            {
                sb.Append($"<b>Item:</b> {m.Item.Name}");
                if (!string.IsNullOrEmpty(m.Item.ShortDesc)) sb.Append($" <size=85%>— {m.Item.ShortDesc}</size>");
                sb.AppendLine();
            }
            else sb.AppendLine("<b>Item:</b> None");

            sb.AppendLine("<b>Moves</b>");
            foreach (var slot in m.Moves)
            {
                var mv = slot.Move;
                string ty = mv.Type != MonType.None ? mv.Type.ToString() : "Status";
                var stats = new List<string>();
                if (mv.BasePower > 0) stats.Add($"{mv.BasePower} BP");
                if (mv.Accuracy > 0) stats.Add($"{mv.Accuracy}%");
                string tail = stats.Count > 0 ? $"  {string.Join(" · ", stats)}" : "";
                sb.AppendLine($"• <b>{mv.Name}</b> <size=85%>({ty}){tail}</size>");
                if (!string.IsNullOrEmpty(mv.ShortDesc)) sb.AppendLine($"<size=80%>{mv.ShortDesc}</size>");
            }
            return sb.ToString().TrimEnd();
        }

        /// <summary>The mon's current types (Tera type when terastallized).</summary>
        public static List<MonType> EffectiveTypes(Pokemon m)
        {
            if (m.IsTerastallized) return new List<MonType> { m.TeraType };
            var list = new List<MonType> { m.Species.Type1 };
            if (m.Species.Type2 != MonType.None) list.Add(m.Species.Type2);
            return list;
        }

        // "Atk 226", or boosted "Atk 339 (×1.5)" in blue / dropped in red.
        static string StatCell(Pokemon m, string label, Stat stat)
        {
            int baseV = m.MaxStats[(int)stat];
            int stage = m.StatStages[(int)stat];
            if (stage == 0) return $"<b>{label}</b> {baseV}";
            float mult = Stats.StageMult(stage);
            int eff = (int)(baseV * mult);
            string col = stage > 0 ? "5AB0FF" : "FF7A7A";
            return $"<b>{label}</b> <color=#{col}>{eff} (×{mult:0.##})</color>";
        }

        static int Pct(Pokemon m)
        {
            int max = m.MaxStats[(int)Stat.HP];
            return max == 0 ? 0 : 100 * m.CurrentHp / max;
        }
    }
}
