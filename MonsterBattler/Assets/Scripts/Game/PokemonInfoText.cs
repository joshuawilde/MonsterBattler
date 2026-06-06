using System.Collections.Generic;
using System.Text;
using MonsterBattler.Sim;

namespace MonsterBattler.Game
{
    /// <summary>
    /// Builds the Showdown-style info blurb for a Pokemon as TMP rich text: header, colored type
    /// badges (via &lt;mark&gt; highlights), stat line, ability (+desc), item, the defensive matchup
    /// (x4/x2/x½/x¼/x0 rows of type badges), and each move with a type badge + power/accuracy + desc.
    /// Pure string work (no Unity), so it's unit-testable.
    /// </summary>
    public static class PokemonInfoText
    {
        public static string Build(Pokemon m)
        {
            if (m == null || m.Species == null) return "";
            var sb = new StringBuilder();

            // Header + types.
            sb.Append($"<size=125%><b>{m.Species.Name}</b></size>   <b>L{m.Level}</b>");
            if (m.IsTerastallized) sb.Append($"   <i>Tera {m.TeraType}</i>");
            sb.AppendLine();
            sb.AppendLine(Badges(Types(m)));

            // Stats — boosted values shown with their stage (blue up / red down). HP has no stage.
            sb.AppendLine($"<b>HP</b> {m.MaxStats[(int)Stat.HP]}  {StatCell(m, "Atk", Stat.Atk)}  {StatCell(m, "Def", Stat.Def)}  " +
                          $"{StatCell(m, "SpA", Stat.SpA)}  {StatCell(m, "SpD", Stat.SpD)}  {StatCell(m, "Spe", Stat.Spe)}");

            // Ability + item.
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

            // Defensive matchup, one row per multiplier (Tera type when terastallized).
            var t1 = m.IsTerastallized ? m.TeraType : m.Species.Type1;
            var t2 = m.IsTerastallized ? MonType.None : m.Species.Type2;
            var matchup = TypeMatchup.Defensive(t1, t2);
            AppendBucket(sb, matchup, 4f, "x4");
            AppendBucket(sb, matchup, 2f, "x2");
            AppendBucket(sb, matchup, 0.5f, "x½");
            AppendBucket(sb, matchup, 0.25f, "x¼");
            AppendBucket(sb, matchup, 0f, "x0");

            sb.AppendLine($"<b>HP:</b> {Pct(m)}%");

            // Moves: type badge + name + power/accuracy, with the description under it.
            sb.AppendLine("<b>Moves</b>");
            foreach (var slot in m.Moves)
            {
                var mv = slot.Move;
                sb.Append(Badge(mv.Type));
                sb.Append($" <b>{mv.Name}</b>");
                var stats = new List<string>();
                if (mv.BasePower > 0) stats.Add($"{mv.BasePower} BP");
                if (mv.Accuracy > 0) stats.Add($"{mv.Accuracy}%");
                if (stats.Count > 0) sb.Append($"   <size=85%>{string.Join(" · ", stats)}</size>");
                sb.AppendLine();
                if (!string.IsNullOrEmpty(mv.ShortDesc)) sb.AppendLine($"<size=80%>{mv.ShortDesc}</size>");
            }
            return sb.ToString().TrimEnd();
        }

        static List<MonType> Types(Pokemon m)
        {
            var list = new List<MonType> { m.Species.Type1 };
            if (m.Species.Type2 != MonType.None) list.Add(m.Species.Type2);
            return list;
        }

        static void AppendBucket(StringBuilder sb, List<TypeMatchup.Entry> matchup, float mult, string label)
        {
            var types = new List<MonType>();
            foreach (var e in matchup) if (e.Multiplier == mult) types.Add(e.Type);
            if (types.Count > 0) sb.AppendLine($"<b>{label}</b>  {Badges(types)}");
        }

        static string Badges(List<MonType> types)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < types.Count; i++) { if (i > 0) sb.Append(' '); sb.Append(Badge(types[i])); }
            return sb.ToString();
        }

        // A colored "badge": the type name highlighted with its type colour, text auto-contrasted.
        static string Badge(MonType type)
        {
            if (type == MonType.None) return "";
            // Opaque badge with white text on every type (matches Showdown).
            return $"<mark=#{TypeColors.Hex(type)}FF><color=#FFFFFF> {type.ToString().ToUpperInvariant()} </color></mark>";
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
