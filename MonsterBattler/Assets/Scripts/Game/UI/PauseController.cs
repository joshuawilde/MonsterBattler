using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterBattler.Game.UI
{
    /// <summary>
    /// Battle pause overlay. Opens on the HUD pause button, freezes the battle (Time.timeScale = 0),
    /// and offers Resume, Forfeit/Quit, and Music / Sound FX / Haptics toggles. The UI is
    /// scene-authored (per the project rule); this only wires behavior to the references. Settings
    /// persist via <see cref="GameSettings"/>; forfeit routes through <see cref="BattleView"/>.
    /// </summary>
    public sealed class PauseController : MonoBehaviour
    {
        [SerializeField] BattleView _battleView;
        [SerializeField] Button _pauseButton;     // small button in the battle HUD
        [SerializeField] GameObject _overlay;      // dim backdrop + card; inactive at rest

        [Header("Card buttons")]
        [SerializeField] Button _resumeButton;
        [SerializeField] Button _forfeitButton;
        [SerializeField] TMP_Text _forfeitLabel;   // "Forfeit" (online) / "Quit Battle" (local)

        [Header("Toggle buttons (label flips On/Off)")]
        [SerializeField] Button _musicButton;
        [SerializeField] TMP_Text _musicLabel;
        [SerializeField] Button _sfxButton;
        [SerializeField] TMP_Text _sfxLabel;
        [SerializeField] Button _hapticsButton;
        [SerializeField] TMP_Text _hapticsLabel;

        bool _paused;

        void Awake()
        {
            if (_battleView == null) _battleView = FindAnyObjectByType<BattleView>();
            Wire(_pauseButton, Open);
            Wire(_resumeButton, Close);
            Wire(_forfeitButton, OnForfeit);
            Wire(_musicButton, () => { GameSettings.MusicEnabled = !GameSettings.MusicEnabled; RefreshLabels(); });
            Wire(_sfxButton, () => { GameSettings.SfxEnabled = !GameSettings.SfxEnabled; RefreshLabels(); });
            Wire(_hapticsButton, () =>
            {
                GameSettings.HapticsEnabled = !GameSettings.HapticsEnabled;
                if (GameSettings.HapticsEnabled) HapticManager.UIClick(); // confirm the buzz is back
                RefreshLabels();
            });
            if (_overlay != null) _overlay.SetActive(false);
        }

        static void Wire(Button b, Action a) { if (b != null) b.onClick.AddListener(() => a()); }

        void Open()
        {
            if (_paused) return;
            if (_battleView != null && !_battleView.InBattle) return; // not during the end screen
            _paused = true;
            RefreshLabels();
            if (_forfeitLabel != null)
                _forfeitLabel.text = (_battleView != null && _battleView.IsOnlineMatch) ? "Forfeit" : "Quit Battle";
            if (_overlay != null) { _overlay.transform.SetAsLastSibling(); _overlay.SetActive(true); }
            Time.timeScale = 0f; // freeze the battle; UI + audio run on unscaled time
        }

        void Close()
        {
            if (!_paused) return;
            _paused = false;
            Time.timeScale = 1f;
            if (_overlay != null) _overlay.SetActive(false);
        }

        void OnForfeit()
        {
            _paused = false;
            Time.timeScale = 1f;
            if (_overlay != null) _overlay.SetActive(false);
            _battleView?.ForfeitMatch();
        }

        void RefreshLabels()
        {
            if (_musicLabel != null) _musicLabel.text = "Music: " + OnOff(GameSettings.MusicEnabled);
            if (_sfxLabel != null) _sfxLabel.text = "Sound FX: " + OnOff(GameSettings.SfxEnabled);
            if (_hapticsLabel != null) _hapticsLabel.text = "Haptics: " + OnOff(GameSettings.HapticsEnabled);
        }

        static string OnOff(bool on) => on ? "On" : "Off";

        // Safety: never leave time frozen if this object is torn down (e.g. scene change) while paused.
        void OnDisable() { if (_paused) Time.timeScale = 1f; }
    }
}
