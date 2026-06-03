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

        public static Color BgColor(MonType t) => t switch
        {
            MonType.Normal   => new Color(0.66f, 0.66f, 0.50f, 1f),
            MonType.Fire     => new Color(0.94f, 0.50f, 0.19f, 1f),
            MonType.Water    => new Color(0.39f, 0.56f, 0.94f, 1f),
            MonType.Electric => new Color(0.97f, 0.82f, 0.18f, 1f),
            MonType.Grass    => new Color(0.47f, 0.78f, 0.31f, 1f),
            MonType.Ice      => new Color(0.59f, 0.85f, 0.85f, 1f),
            MonType.Fighting => new Color(0.75f, 0.18f, 0.16f, 1f),
            MonType.Poison   => new Color(0.64f, 0.24f, 0.63f, 1f),
            MonType.Ground   => new Color(0.88f, 0.75f, 0.41f, 1f),
            MonType.Flying   => new Color(0.66f, 0.56f, 0.95f, 1f),
            MonType.Psychic  => new Color(0.97f, 0.34f, 0.53f, 1f),
            MonType.Bug      => new Color(0.65f, 0.72f, 0.10f, 1f),
            MonType.Rock     => new Color(0.71f, 0.63f, 0.21f, 1f),
            MonType.Ghost    => new Color(0.45f, 0.34f, 0.59f, 1f),
            MonType.Dragon   => new Color(0.44f, 0.21f, 0.99f, 1f),
            MonType.Dark     => new Color(0.44f, 0.34f, 0.27f, 1f),
            MonType.Steel    => new Color(0.72f, 0.72f, 0.81f, 1f),
            MonType.Fairy    => new Color(0.93f, 0.60f, 0.67f, 1f),
            MonType.Stellar  => new Color(0.55f, 0.85f, 0.80f, 1f),
            _                => new Color(0.5f, 0.5f, 0.5f, 1f),
        };
    }
}
