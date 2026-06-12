using System.Collections.Generic;
using UnityEngine;

namespace MonsterBattler.Game
{
    /// <summary>
    /// Music + SFX hub. The battle theme is three PHASE-LOCKED stems (base/tension/triumph —
    /// identical length and tempo, all playing simultaneously on synced sources) so
    /// <see cref="SetBattleMood"/> crossfades volumes mid-phrase without losing musical time:
    /// player down to last mon → tension mix, opponent on last → triumph mix. Music pitch follows
    /// Time.timeScale so the KO slow-mo warps the music for free. Clips + sources are
    /// scene/editor-wired (tools/build-audio.py); playback only toggles volumes.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }

        public enum Mood { Base, Tension, Triumph }

        [Header("Sources (scene-wired)")]
        [SerializeField] AudioSource _menuSrc;       // menu loop + end stings
        [SerializeField] AudioSource _stemBase;
        [SerializeField] AudioSource _stemTension;
        [SerializeField] AudioSource _stemTriumph;
        [SerializeField] AudioSource _sfxSrc;        // one-shots

        [Header("Music clips")]
        [SerializeField] AudioClip _menuTheme;
        [SerializeField] AudioClip _battleBase;
        [SerializeField] AudioClip _battleTension;
        [SerializeField] AudioClip _battleTriumph;
        [SerializeField] AudioClip _stingVictory;
        [SerializeField] AudioClip _stingDefeat;

        [Header("SFX clips")]
        [SerializeField] AudioClip _click;
        [SerializeField] AudioClip _hit;
        [SerializeField] AudioClip _hitSuper;
        [SerializeField] AudioClip _hitWeak;
        [SerializeField] AudioClip _faint;
        [SerializeField] AudioClip _switch;
        [SerializeField] AudioClip _heal;
        [SerializeField] AudioClip _status;
        [SerializeField] AudioClip _boost;
        [SerializeField] AudioClip _unboost;
        [SerializeField] AudioClip _hazard;
        [SerializeField] AudioClip _itemOff;
        [SerializeField] AudioClip _levelUp;
        [SerializeField] AudioClip _charge;

        const float MusicVol = 0.55f;
        const float FadeSpeed = 1.6f;     // mood crossfade, volume units/sec

        readonly Dictionary<string, AudioClip> _sfx = new();
        float _vBase, _vTension, _vTriumph; // crossfade targets
        bool _battlePlaying;

        void Awake()
        {
            I = this;
            void Add(string n, AudioClip c) { if (c != null) _sfx[n] = c; }
            Add("click", _click); Add("hit", _hit); Add("hit_super", _hitSuper); Add("hit_weak", _hitWeak);
            Add("faint", _faint); Add("switch", _switch); Add("heal", _heal); Add("status", _status);
            Add("boost", _boost); Add("unboost", _unboost); Add("hazard", _hazard);
            Add("item_off", _itemOff); Add("levelup", _levelUp); Add("charge", _charge);

            // Click sound on every scene-authored button (dynamic cells call Play("click") directly).
            foreach (var b in FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                b.onClick.AddListener(() => Play("click"));
        }

        void Update()
        {
            // Mood crossfade + slow-mo pitch warp (KO moments dip Time.timeScale).
            float dt = Time.unscaledDeltaTime * FadeSpeed;
            if (_stemBase != null) _stemBase.volume = Mathf.MoveTowards(_stemBase.volume, _vBase, dt);
            if (_stemTension != null) _stemTension.volume = Mathf.MoveTowards(_stemTension.volume, _vTension, dt);
            if (_stemTriumph != null) _stemTriumph.volume = Mathf.MoveTowards(_stemTriumph.volume, _vTriumph, dt);
            float pitch = Mathf.Clamp(Time.timeScale, 0.3f, 1f);
            if (_stemBase != null) _stemBase.pitch = pitch;
            if (_stemTension != null) _stemTension.pitch = pitch;
            if (_stemTriumph != null) _stemTriumph.pitch = pitch;
        }

        public static void Play(string name)
        {
            if (I == null || I._sfxSrc == null || !I._sfx.TryGetValue(name, out var clip)) return;
            I._sfxSrc.PlayOneShot(clip, 0.85f);
        }

        public void PlayMenuMusic()
        {
            StopBattleStems();
            if (_menuSrc == null || _menuTheme == null) return;
            if (_menuSrc.clip == _menuTheme && _menuSrc.isPlaying) return;
            _menuSrc.clip = _menuTheme; _menuSrc.loop = true; _menuSrc.volume = MusicVol * 0.8f;
            _menuSrc.Play();
        }

        public void PlayBattleMusic()
        {
            if (_menuSrc != null) _menuSrc.Stop();
            if (_stemBase == null || _battleBase == null) return;
            Prep(_stemBase, _battleBase); Prep(_stemTension, _battleTension); Prep(_stemTriumph, _battleTriumph);
            _vBase = MusicVol; _vTension = 0f; _vTriumph = 0f;
            _stemBase.volume = MusicVol;
            // PlayScheduled on a shared DSP time keeps the three stems sample-locked.
            double at = AudioSettings.dspTime + 0.1;
            _stemBase.PlayScheduled(at);
            if (_stemTension != null && _battleTension != null) _stemTension.PlayScheduled(at);
            if (_stemTriumph != null && _battleTriumph != null) _stemTriumph.PlayScheduled(at);
            _battlePlaying = true;

            static void Prep(AudioSource s, AudioClip c)
            {
                if (s == null || c == null) return;
                s.clip = c; s.loop = true; s.volume = 0f; s.pitch = 1f;
            }
        }

        /// <summary>Crossfade the battle mix: tension when the player is on their last mon,
        /// triumph when the opponent is. Keeps musical time (stems stay in sync).</summary>
        public void SetBattleMood(Mood mood)
        {
            if (!_battlePlaying) return;
            _vBase = mood == Mood.Base ? MusicVol : MusicVol * 0.18f;
            _vTension = mood == Mood.Tension ? MusicVol * 1.1f : 0f;
            _vTriumph = mood == Mood.Triumph ? MusicVol : 0f;
        }

        public void PlayVictory() => EndBattle(_stingVictory);
        public void PlayDefeat() => EndBattle(_stingDefeat);

        void EndBattle(AudioClip sting)
        {
            StopBattleStems();
            if (_menuSrc != null && sting != null)
            {
                _menuSrc.Stop(); _menuSrc.clip = null;
                _menuSrc.volume = MusicVol;
                _menuSrc.PlayOneShot(sting, 0.9f);
            }
        }

        void StopBattleStems()
        {
            _battlePlaying = false;
            if (_stemBase != null) _stemBase.Stop();
            if (_stemTension != null) _stemTension.Stop();
            if (_stemTriumph != null) _stemTriumph.Stop();
        }
    }
}
