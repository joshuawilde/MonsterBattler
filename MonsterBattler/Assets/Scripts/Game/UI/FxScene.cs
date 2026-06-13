using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MonsterBattler.Game.UI
{
    /// <summary>
    /// Unity port of Pokémon Showdown's two animation primitives (battle-animations.ts):
    ///   ShowEffect(sprite, from, to, ease, fade) — tween one fx sprite between two states
    ///   BackgroundEffect(color, duration, opacity) — full-screen tint flash
    /// Choreographies (MoveAnims) compose these. The fx sprite prefab + sprite library + flash
    /// overlay are scene/editor-wired per the project authoring rule; playback only instantiates.
    /// </summary>
    public sealed class FxScene : MonoBehaviour
    {
        [SerializeField] SpriteRenderer _fxPrefab;        // world-space fx sprite template (inactive in-scene)
        [SerializeField] UnityEngine.UI.Image _flash;     // full-screen overlay (alpha 0 at rest)

        [Header("Fx sprite library (editor-wired assets)")]
        [SerializeField] Sprite _orb;
        [SerializeField] Sprite _ring;
        [SerializeField] Sprite _lightning;
        [SerializeField] Sprite _fist;
        [SerializeField] Sprite _impact;
        [SerializeField] Sprite _slash;
        [SerializeField] Sprite _icicle;
        [SerializeField] Sprite _leaf;
        [SerializeField] Sprite _rock;
        [SerializeField] Sprite _item;
        [SerializeField] Sprite _web;
        [SerializeField] Sprite _spike;

        readonly Dictionary<string, Sprite> _byName = new();
        int _running;

        /// <summary>True while any effect or flash is still animating.</summary>
        public bool Busy => _running > 0;

        void Awake()
        {
            void Add(string n, Sprite s) { if (s != null) _byName[n] = s; }
            Add("orb", _orb); Add("ring", _ring); Add("lightning", _lightning);
            Add("fist", _fist); Add("impact", _impact); Add("slash", _slash);
            Add("icicle", _icicle); Add("leaf", _leaf); Add("rock", _rock);
            Add("item", _item); Add("web", _web); Add("spike", _spike);
        }

        public struct State
        {
            public Vector3 pos;       // world position
            public float scale;       // uniform; xscale/yscale multiply it
            public float xscale, yscale;
            public float opacity;
            public float timeMs;      // PS semantics: from.timeMs = start delay, to.timeMs = end time
            public Color tint;

            public static State At(Vector3 p) => new()
            { pos = p, scale = 1f, xscale = 1f, yscale = 1f, opacity = 1f, timeMs = 0f, tint = Color.white };

            public State Pos(Vector3 p) { var s = this; s.pos = p; return s; }
            public State Scale(float v) { var s = this; s.scale = v; return s; }
            public State XScale(float v) { var s = this; s.xscale = v; return s; }
            public State YScale(float v) { var s = this; s.yscale = v; return s; }
            public State Alpha(float v) { var s = this; s.opacity = v; return s; }
            public State Time(float ms) { var s = this; s.timeMs = ms; return s; }
            public State Tint(Color c) { var s = this; s.tint = c; return s; }
        }

        public enum Fade { Fade, Decel, Linear, Explode, Gone }   // how the sprite resolves at the end
        public enum Ease { Linear, Swing, Accel, Decel }          // tween timing curve

        /// <summary>Spawn one fx sprite and tween it from → to (PS showEffect).</summary>
        public void ShowEffect(string sprite, State from, State to, Fade fade = Fade.Fade)
            => ShowEffect(sprite, from, to, fade == Fade.Decel ? Ease.Decel : Ease.Linear, fade);

        /// <summary>Full form: explicit ease + fade + optional ballistic y-arc (world units; negative dips under).</summary>
        public void ShowEffect(string sprite, State from, State to, Ease ease, Fade fade, float arcY = 0f)
        {
            if (_fxPrefab == null || !_byName.TryGetValue(sprite, out var spr)) return;
            StartCoroutine(RunEffect(sprite, spr, from, to, ease, fade, arcY));
        }

        IEnumerator RunEffect(string name, Sprite spr, State from, State to, Ease ease, Fade fade, float arcY)
        {
            _running++;
            if (from.timeMs > 0f) yield return new WaitForSeconds(from.timeMs / 1000f);
            HapticManager.Effect(name, fade == Fade.Explode); // keyframed buzz the instant the sprite lands

            var sr = Instantiate(_fxPrefab, transform);
            sr.gameObject.SetActive(true);
            sr.sprite = spr;

            float dur = Mathf.Max(0.01f, (to.timeMs - from.timeMs) / 1000f);
            for (float t = 0f; t < dur; t += UnityEngine.Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / dur);
                k = ease switch
                {
                    Ease.Swing => 0.5f - Mathf.Cos(k * Mathf.PI) * 0.5f,
                    Ease.Accel => k * k,
                    Ease.Decel => 1f - (1f - k) * (1f - k),
                    _ => k,
                };
                Apply(sr, from, to, k, fade, arcY);
                yield return null;
            }
            Destroy(sr.gameObject);
            _running--;
        }

        static void Apply(SpriteRenderer sr, State a, State b, float k, Fade fade, float arcY)
        {
            var pos = Vector3.Lerp(a.pos, b.pos, k);
            pos.y += arcY * 4f * k * (1f - k); // parabolic arc for thrown/ballistic sprites
            sr.transform.position = pos;
            float tail = fade is Fade.Fade or Fade.Explode && k > 0.7f ? (k - 0.7f) / 0.3f : 0f;
            float sx = Mathf.Lerp(a.scale * a.xscale, b.scale * b.xscale, k);
            float sy = Mathf.Lerp(a.scale * a.yscale, b.scale * b.yscale, k);
            if (fade == Fade.Explode) { sx *= 1f + 0.8f * tail; sy *= 1f + 0.8f * tail; }
            sr.transform.localScale = new Vector3(sx, sy, 1f);
            float alpha = Mathf.Lerp(a.opacity, b.opacity, k) * (1f - tail);
            var c = Color.Lerp(a.tint, b.tint, k);
            c.a = alpha;
            sr.color = c;
        }

        /// <summary>Full-screen tint flash (PS backgroundEffect).</summary>
        public void BackgroundEffect(Color color, float durationMs, float opacity, float delayMs = 0f)
        {
            if (_flash == null) return;
            StartCoroutine(RunFlash(color, durationMs / 1000f, opacity, delayMs / 1000f));
        }

        IEnumerator RunFlash(Color color, float dur, float opacity, float delay)
        {
            _running++;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            HapticManager.Flash(opacity); // keyframed thud on the flash beat (explosions, lightning bg)
            float half = dur / 2f;
            for (float t = 0f; t < dur; t += UnityEngine.Time.deltaTime)
            {
                float a = t < half ? t / half : 1f - (t - half) / half;
                var c = color; c.a = opacity * Mathf.Clamp01(a);
                _flash.color = c;
                yield return null;
            }
            _flash.color = new Color(0, 0, 0, 0);
            _running--;
        }

        /// <summary>Wait helper for choreographies: yields until all queued pieces finish.</summary>
        public IEnumerator WaitDone(float maxSeconds = 3f)
        {
            float t = 0f;
            while (Busy && t < maxSeconds) { t += UnityEngine.Time.deltaTime; yield return null; }
        }

        // ---- impact layer: screen shake / hit-stop / KO slow-mo -------------------------------

        Coroutine _shakeCo;

        /// <summary>Decaying camera shake; strength in world units (~0.05 light, 0.15 heavy).</summary>
        public void Shake(float strength, float duration = 0.3f)
        {
            var cam = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
            if (cam == null) return;
            if (_shakeCo != null) StopCoroutine(_shakeCo);
            _shakeCo = StartCoroutine(RunShake(cam.transform, strength, duration));
        }

        static IEnumerator RunShake(Transform cam, float strength, float duration)
        {
            Vector3 last = Vector3.zero;
            for (float t = 0f; t < duration; t += UnityEngine.Time.unscaledDeltaTime)
            {
                float falloff = 1f - t / duration;
                var off = new Vector3(
                    (Mathf.PerlinNoise(t * 35f, 0.3f) - 0.5f) * 2f,
                    (Mathf.PerlinNoise(0.7f, t * 35f) - 0.5f) * 2f, 0f) * (strength * falloff);
                cam.position += off - last;
                last = off;
                yield return null;
            }
            cam.position -= last;
        }

        /// <summary>Brief freeze-frame on impact (unscaled-time safe).</summary>
        public void HitStop(float seconds = 0.07f) => StartCoroutine(RunTimeScale(0.02f, seconds));

        /// <summary>KO moment: short slow-motion + white flash.</summary>
        public void KoMoment()
        {
            StartCoroutine(RunTimeScale(0.3f, 0.45f));
            BackgroundEffect(Color.white, 350f, 0.5f);
            Shake(0.12f, 0.4f);
        }

        static bool _timeWarped;
        static IEnumerator RunTimeScale(float scale, float seconds)
        {
            if (_timeWarped) yield break; // don't stack warps
            _timeWarped = true;
            float prev = Time.timeScale;
            Time.timeScale = scale;
            float t = 0f;
            while (t < seconds) { t += UnityEngine.Time.unscaledDeltaTime; yield return null; }
            Time.timeScale = prev;
            _timeWarped = false;
        }
    }
}
