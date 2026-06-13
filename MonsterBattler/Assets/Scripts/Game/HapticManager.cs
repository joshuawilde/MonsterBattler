using UnityCoreHaptics;
using UnityEngine;

namespace MonsterBattler.Game
{
    /// <summary>
    /// Battle haptics via iOS Core Haptics (UnityCoreHaptics plugin). Rich signature moments
    /// (KO, crit, victory…) play authored AHAP files from StreamingAssets/Haptics; variable hits
    /// and move "casts" use parameterized transient/continuous patterns. Every entry point no-ops
    /// off-iOS / on unsupported devices / when disabled — <see cref="UnityCoreHapticsProxy"/>'s own
    /// methods are platform-guarded, and <see cref="Ready"/> gates on support + the user pref.
    /// Call <see cref="Init"/> once at battle start to spin up the engine (avoids a first-play spike).
    /// </summary>
    public static class HapticManager
    {
        public static bool Enabled = true;       // mirrors GameSettings.HapticsEnabled
        static bool _initialized, _supported;

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            Enabled = GameSettings.HapticsEnabled;
            _supported = UnityCoreHapticsProxy.SupportsCoreHaptics(); // false off-iOS
            if (_supported) UnityCoreHapticsProxy.CreateEngine();
        }

        static bool Ready
        {
            get
            {
                if (!_initialized) Init();
                return _supported && Enabled;
            }
        }

        // ---- semantic battle API (intensity 0..1, sharpness 0..1) ------------------------------

        /// <summary>Light tap for UI taps / minor beats.</summary>
        public static void UIClick() => Transient(0.35f, 0.6f);

        /// <summary>A move firing — flavored by its type so it reads like the move (Ground rumbles,
        /// Electric snaps, Fire whooshes…). Played at cast time, alongside the move's VFX; the
        /// landing damage adds its own <see cref="Hit"/> on top.</summary>
        public static void Move(string typeName)
        {
            switch ((typeName ?? "").ToLowerInvariant())
            {
                case "ground": case "rock":      Continuous(0.95f, 0.18f, 0.38f); break; // heavy rumble (earthquake)
                case "electric":                 Transient(0.95f, 1.0f); break;          // sharp zap (lightning)
                case "fire":                     Continuous(0.75f, 0.6f, 0.22f); break;   // burst/whoosh
                case "water":                    Continuous(0.55f, 0.35f, 0.25f); break;  // surge
                case "ice":                      Transient(0.7f, 0.85f); break;           // sharp crack
                case "fighting": case "steel":   Transient(1.0f, 0.8f); break;            // hard strike
                case "dragon": case "dark":      Continuous(0.85f, 0.4f, 0.28f); break;   // ominous growl
                case "grass": case "bug":        Transient(0.45f, 0.45f); break;          // soft
                case "psychic": case "fairy":    Transient(0.5f, 0.75f); break;           // shimmer
                case "ghost": case "poison":     Continuous(0.5f, 0.3f, 0.3f); break;     // eerie
                case "flying":                   Transient(0.45f, 0.6f); break;           // gust
                default:                          Transient(0.55f, 0.55f); break;
            }
        }

        /// <summary>An incoming hit; severity 0 (chip) → 1 (huge chunk) scales the thump.
        /// Big hits play the richer thud pattern.</summary>
        public static void Hit(float severity)
        {
            severity = Mathf.Clamp01(severity);
            if (severity >= 0.6f) { if (!PlayFile("Haptics/hit_heavy.ahap")) Transient(1f, 0.7f); }
            else Transient(Mathf.Lerp(0.35f, 0.85f, severity), Mathf.Lerp(0.4f, 0.8f, severity));
        }

        /// <summary>Critical hit — sharp double snap.</summary>
        public static void Crit() { if (!PlayFile("Haptics/crit.ahap")) Transient(0.9f, 1f); }

        /// <summary>Super-effective / very heavy blow.</summary>
        public static void SuperEffective() { if (!PlayFile("Haptics/supereffective.ahap")) Transient(1f, 0.9f); }

        /// <summary>Knock-out — the big rumble (swell + sharp impact + fade).</summary>
        public static void Ko() { if (!PlayFile("Haptics/ko.ahap")) Continuous(1f, 0.3f, 0.45f); }

        /// <summary>Victory flourish.</summary>
        public static void Victory() { if (!PlayFile("Haptics/victory.ahap")) Transient(0.8f, 0.6f); }

        /// <summary>Defeat — low, sinking rumble.</summary>
        public static void Defeat() { if (!PlayFile("Haptics/defeat.ahap")) Continuous(0.7f, 0.15f, 0.5f); }

        /// <summary>Level-up — rising ticks.</summary>
        public static void LevelUp() { if (!PlayFile("Haptics/levelup.ahap")) Transient(0.6f, 0.8f); }

        // ---- primitives -----------------------------------------------------------------------

        public static void Transient(float intensity, float sharpness)
        {
            if (!Ready) return;
            UnityCoreHapticsProxy.PlayTransientHaptics(Mathf.Clamp01(intensity), Mathf.Clamp01(sharpness));
        }

        public static void Continuous(float intensity, float sharpness, float duration)
        {
            if (!Ready) return;
            UnityCoreHapticsProxy.PlayContinuousHaptics(Mathf.Clamp01(intensity), Mathf.Clamp01(sharpness), duration);
        }

        /// <summary>Play an AHAP file under StreamingAssets; returns false if missing so callers can
        /// fall back to a transient. Returns true (handled) when haptics are off/unsupported.</summary>
        static bool PlayFile(string relativePath)
        {
            if (!Ready) return true;
            if (!Utility.FileExists(relativePath)) return false;
            UnityCoreHapticsProxy.PlayHapticsFromFile(relativePath);
            return true;
        }
    }
}
