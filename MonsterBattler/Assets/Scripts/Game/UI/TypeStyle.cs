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

        /// <summary>Three-letter type tag for compact UI (roster pips, etc.).</summary>
        public static string Abbrev(MonType t) => t switch
        {
            MonType.Normal => "NRM", MonType.Fire => "FIR", MonType.Water => "WTR", MonType.Electric => "ELE",
            MonType.Grass => "GRS", MonType.Ice => "ICE", MonType.Fighting => "FGT", MonType.Poison => "PSN",
            MonType.Ground => "GRD", MonType.Flying => "FLY", MonType.Psychic => "PSY", MonType.Bug => "BUG",
            MonType.Rock => "RCK", MonType.Ghost => "GHO", MonType.Dragon => "DRG", MonType.Dark => "DRK",
            MonType.Steel => "STL", MonType.Fairy => "FAI", _ => "",
        };

        public static Color BgColor(MonType t)
        {
            var (r, g, b) = TypeColors.Rgb(t);
            return new Color(r, g, b, 1f);
        }
    }
}
