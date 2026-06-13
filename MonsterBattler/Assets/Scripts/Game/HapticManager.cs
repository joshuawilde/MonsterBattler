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

        /// <summary>Keyframed VFX haptic: fired by <see cref="UI.FxScene"/> at the exact frame each
        /// effect sprite appears, so the buzz lands on the visual beat (the lightning bolt snaps, the
        /// fist connects, the rock thuds). Flavored by which library sprite it is; projectiles/
        /// cosmetics (orb/leaf/item) stay silent so we don't buzz on every in-flight frame.
        /// explode = a Fade.Explode burst → a short boom regardless of sprite.</summary>
        public static void Effect(string librarySprite, bool explode)
        {
            if (explode) { Continuous(0.9f, 0.3f, 0.16f); return; } // explosion boom
            switch (librarySprite)
            {
                case "impact":    Transient(0.85f, 0.9f); break;  // burst hit
                case "fist":      Transient(1.0f, 0.7f); break;   // heavy punch
                case "slash":     Transient(0.8f, 1.0f); break;   // sharp slash
                case "lightning": Transient(0.95f, 1.0f); break;  // zap (Thunderbolt's bolt)
                case "icicle":    Transient(0.7f, 0.9f); break;   // crack
                case "rock":      Transient(0.9f, 0.4f); break;   // earthy thud (Earthquake)
                case "spike":     Transient(0.5f, 0.6f); break;
                case "web":       Transient(0.35f, 0.5f); break;
                case "ring":      Transient(0.5f, 0.55f); break;  // generic burst/shimmer (light)
                // orb, leaf, item → silent (projectiles / cosmetics)
            }
        }

        /// <summary>Keyframed full-screen flash (explosions, lightning bg) → a low thud. Skips faint
        /// tints so subtle background washes don't buzz.</summary>
        public static void Flash(float opacity)
        {
            if (opacity >= 0.25f) Continuous(Mathf.Clamp01(0.45f + opacity), 0.22f, 0.16f);
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

        /// <summary>Celebration buzz for the unlock/level-up pop (rising ticks).</summary>
        public static void Celebrate() { if (!PlayFile("Haptics/levelup.ahap")) Transient(0.6f, 0.8f); }

        // ---- continuous "meter" player: a sustained rumble whose intensity we ramp live (used while
        //      the end-screen XP / progress bars fill). Begin → Update each frame → End. ------------

        static int _meterPlayer = -1;

        public static void MeterBegin(float intensity = 0.15f, float sharpness = 0.3f)
        {
            MeterEnd(); // never leak a prior player
            if (!Ready) return;
            _meterPlayer = UnityCoreHapticsProxy.CreateContinuousPlayer(Mathf.Clamp01(intensity), Mathf.Clamp01(sharpness));
            UnityCoreHapticsProxy.StartPlayer(_meterPlayer);
        }

        public static void MeterUpdate(float intensity, float sharpness)
        {
            if (_meterPlayer < 0) return;
            UnityCoreHapticsProxy.UpdatePlayerParameters(_meterPlayer, Mathf.Clamp01(intensity), Mathf.Clamp01(sharpness));
        }

        public static void MeterEnd()
        {
            if (_meterPlayer < 0) return;
            UnityCoreHapticsProxy.StopPlayer(_meterPlayer);
            UnityCoreHapticsProxy.DestroyPlayer(_meterPlayer);
            _meterPlayer = -1;
        }

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
