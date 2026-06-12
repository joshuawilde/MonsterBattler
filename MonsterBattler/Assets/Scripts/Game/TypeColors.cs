using MonsterBattler.Sim;

namespace MonsterBattler.Game
{
    /// <summary>
    /// Per-type display colors as plain RGB (no UnityEngine), so both the UI (TypeStyle → Color) and
    /// pure text builders (PokemonInfoText → hex for &lt;mark&gt; badges) share one source of truth.
    /// Roughly matches Pokemon Showdown's type tinting.
    /// </summary>
    public static class TypeColors
    {
        public static (float r, float g, float b) Rgb(MonType t) => t switch
        {
            MonType.Normal   => (0.66f, 0.66f, 0.50f),
            MonType.Fire     => (0.94f, 0.50f, 0.19f),
            MonType.Water    => (0.39f, 0.56f, 0.94f),
            MonType.Electric => (0.97f, 0.82f, 0.18f),
            MonType.Grass    => (0.47f, 0.78f, 0.31f),
            MonType.Ice      => (0.59f, 0.85f, 0.85f),
            MonType.Fighting => (0.75f, 0.18f, 0.16f),
            MonType.Poison   => (0.64f, 0.24f, 0.63f),
            MonType.Ground   => (0.88f, 0.75f, 0.41f),
            MonType.Flying   => (0.66f, 0.56f, 0.95f),
            MonType.Psychic  => (0.97f, 0.34f, 0.53f),
            MonType.Bug      => (0.65f, 0.72f, 0.10f),
            MonType.Rock     => (0.71f, 0.63f, 0.21f),
            MonType.Ghost    => (0.45f, 0.34f, 0.59f),
            MonType.Dragon   => (0.44f, 0.21f, 0.99f),
            MonType.Dark     => (0.44f, 0.34f, 0.27f),
            MonType.Steel    => (0.72f, 0.72f, 0.81f),
            MonType.Fairy    => (0.93f, 0.60f, 0.67f),
            MonType.Stellar  => (0.55f, 0.85f, 0.80f),
            _                => (0.50f, 0.50f, 0.50f),
        };

        public static string Hex(MonType t)
        {
            var (r, g, b) = Rgb(t);
            return $"{(int)(r * 255):X2}{(int)(g * 255):X2}{(int)(b * 255):X2}";
        }

        /// <summary>Perceived brightness 0..1 — use to pick dark vs light text on the badge.</summary>
        public static float Luminance(MonType t)
        {
            var (r, g, b) = Rgb(t);
            return 0.30f * r + 0.59f * g + 0.11f * b;
        }

        /// <summary>Hex for move-category words (Physical orange / Special blue / Status gray).</summary>
        public static string CategoryHex(MoveCategory cat) => cat switch
        {
            MoveCategory.Physical => "F47B20",
            MoveCategory.Special => "5A8CF0",
            _ => "9CA0AA",
        };
    }
}
