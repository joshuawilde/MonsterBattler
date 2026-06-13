using UnityEngine;

namespace MonsterBattler.Game
{
    /// <summary>
    /// Device-local preferences (music / sfx / haptics on-off). Stored in PlayerPrefs, NOT the
    /// cloud-synced <see cref="Meta.PlayerProfile"/> — these are per-device settings (you might want
    /// sound off on the phone but on at the desk), so they shouldn't ride the save's last-write-wins.
    /// </summary>
    public static class GameSettings
    {
        public static bool MusicEnabled
        {
            get => GetBool("opt_music", true);
            set { SetBool("opt_music", value); AudioManager.I?.ApplyAudioSettings(); }
        }

        public static bool SfxEnabled
        {
            get => GetBool("opt_sfx", true);
            set { SetBool("opt_sfx", value); AudioManager.I?.ApplyAudioSettings(); }
        }

        public static bool HapticsEnabled
        {
            get => GetBool("opt_haptics", true);
            set { SetBool("opt_haptics", value); HapticManager.Enabled = value; }
        }

        static bool GetBool(string key, bool dflt) => PlayerPrefs.GetInt(key, dflt ? 1 : 0) != 0;
        static void SetBool(string key, bool v) { PlayerPrefs.SetInt(key, v ? 1 : 0); PlayerPrefs.Save(); }
    }
}
