using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using UnityCoreHaptics;
#endif

namespace MonsterBattler.Game
{
    /// <summary>
    /// Battle haptics via iOS Core Haptics (UnityCoreHaptics plugin). Rich signature moments
    /// (KO, crit, victory…) play authored AHAP files from StreamingAssets/Haptics; variable hits
    /// use scaled transient taps. No-ops on non-iOS / unsupported devices / when disabled.
    /// Call <see cref="Init"/> once at battle start to spin up the engine (avoids a first-play
    /// frame spike). All public methods are safe to call from anywhere on the main thread.
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
#if UNITY_IOS && !UNITY_EDITOR
            _supported = UnityCoreHapticsProxy.SupportsCoreHaptics();
            if (_supported) UnityCoreHapticsProxy.CreateEngine();
#endif
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

        /// <summary>An incoming hit; severity 0 (chip) → 1 (huge chunk) scales the thump.
        /// Big hits play the richer thud pattern.</summary>
        public static void Hit(float severity)
        {
            severity = Mathf.Clamp01(severity);
            if (severity >= 0.6f) { if (!PlayFile("Haptics/hit_heavy.ahap")) Transient(1f, 0.7f); }
            else Transient(Mathf.Lerp(0.35f, 0.85f, severity), Mathf.Lerp(0.4f, 0.8f, severity));
        }

        /// <summary>Critical hit — sharp double snap.</summary>
        public static void Crit() { if (!PlayFile("Haptics/crit.ahap")) { Transient(0.9f, 1f); } }

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
#if UNITY_IOS && !UNITY_EDITOR
            UnityCoreHapticsProxy.PlayTransientHaptics(Mathf.Clamp01(intensity), Mathf.Clamp01(sharpness));
#endif
        }

        public static void Continuous(float intensity, float sharpness, float duration)
        {
            if (!Ready) return;
#if UNITY_IOS && !UNITY_EDITOR
            UnityCoreHapticsProxy.PlayContinuousHaptics(Mathf.Clamp01(intensity), Mathf.Clamp01(sharpness), duration);
#endif
        }

        /// <summary>Play an AHAP file under StreamingAssets; returns false if missing/unavailable so
        /// callers can fall back to a transient.</summary>
        static bool PlayFile(string relativePath)
        {
            if (!Ready) return true; // treat as "handled" (no-op) so we don't also fire a fallback
#if UNITY_IOS && !UNITY_EDITOR
            if (!UnityCoreHaptics.Utility.FileExists(relativePath)) return false;
            UnityCoreHapticsProxy.PlayHapticsFromFile(relativePath);
            return true;
#else
            return true;
#endif
        }
    }
}
