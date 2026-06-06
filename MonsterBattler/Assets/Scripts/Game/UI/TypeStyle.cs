using MonsterBattler.Sim;
using UnityEngine;

namespace MonsterBattler.Game.UI
{
    /// <summary>
    /// Maps <see cref="MonType"/> to display name + background color. Colors are picked to
    /// roughly match Pokemon Showdown's per-type tinting so the UI feels familiar.
    /// </summary>
    public static class TypeStyle
    {
        public static string Display(MonType t) => t == MonType.None ? "" : t.ToString();

        public static Color BgColor(MonType t)
        {
            var (r, g, b) = TypeColors.Rgb(t);
            return new Color(r, g, b, 1f);
        }
    }
}
