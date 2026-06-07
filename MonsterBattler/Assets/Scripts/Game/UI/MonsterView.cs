using System.Collections;
using UnityEngine;

namespace MonsterBattler.Game.UI
{
    /// <summary>
    /// The on-field monster as a world-space <see cref="SpriteRenderer"/> (billboarded upright toward
    /// the camera). Plays Showdown-style tween animations on the static pixel sprite: enter slide+fade,
    /// attack lunge toward the foe, hit shake+flash, faint drop+fade. _isPlayerSide picks the back
    /// sprite and the lunge direction.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class MonsterView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer _renderer;
        [SerializeField] bool _isPlayerSide;

        Vector3 _home;
        Coroutine _co;

        void Awake()
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            _home = transform.localPosition;
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 toCam = cam.transform.position - transform.position;
            toCam.y = 0f; // upright billboard (yaw only)
            if (toCam.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(toCam);
        }

        // Lunge toward the foe: player (front-left slot) pushes +x/+z, opponent the opposite.
        Vector3 Lunge => (_isPlayerSide ? new Vector3(0.5f, 0f, 0.4f) : new Vector3(-0.5f, 0f, -0.4f));

        public void SetSpecies(string speciesId)
        {
            var sprite = MonSpriteLoader.Load(speciesId, _isPlayerSide);
            _renderer.sprite = sprite;
            _renderer.enabled = sprite != null;
            _renderer.color = Color.white;
            transform.localPosition = _home;
        }

        public void PlayEnter() => Run(EnterCo());
        public void PlayAttack() => Run(AttackCo());
        public void PlayUse() => Run(UseCo());   // self/status move — bob in place, no forward lunge
        public void PlayHit() => Run(HitCo());
        public void PlayFaint() => Run(FaintCo());

        void Run(IEnumerator co)
        {
            if (!isActiveAndEnabled || _renderer == null || _renderer.sprite == null) return;
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(co);
        }

        IEnumerator EnterCo()
        {
            _renderer.enabled = true;
            Vector3 from = _home - Lunge.normalized * 3f;
            yield return Tween(0.32f, t =>
            {
                transform.localPosition = Vector3.Lerp(from, _home, EaseOut(t));
                SetAlpha(t);
            });
            transform.localPosition = _home; SetAlpha(1f);
        }

        IEnumerator AttackCo()
        {
            Vector3 lunged = _home + Lunge;
            yield return Tween(0.12f, t => transform.localPosition = Vector3.Lerp(_home, lunged, t));
            yield return Tween(0.20f, t => transform.localPosition = Vector3.Lerp(lunged, _home, EaseOut(t)));
            transform.localPosition = _home;
        }

        IEnumerator UseCo()
        {
            Vector3 up = _home + new Vector3(0f, 0.22f, 0f);
            yield return Tween(0.15f, t => transform.localPosition = Vector3.Lerp(_home, up, t));
            yield return Tween(0.22f, t => transform.localPosition = Vector3.Lerp(up, _home, EaseOut(t)));
            transform.localPosition = _home;
        }

        IEnumerator HitCo()
        {
            const float dur = 0.34f;
            float e = 0f;
            while (e < dur)
            {
                e += Time.deltaTime;
                float k = 1f - e / dur;
                transform.localPosition = _home + new Vector3(Mathf.Sin(e * 80f) * 0.18f * k, 0f, 0f);
                _renderer.color = Color.Lerp(Color.white, new Color(1f, 0.4f, 0.4f), Mathf.PingPong(e * 12f, 1f) * k);
                yield return null;
            }
            transform.localPosition = _home; _renderer.color = Color.white;
        }

        IEnumerator FaintCo()
        {
            yield return Tween(0.5f, t =>
            {
                transform.localPosition = _home + new Vector3(0f, -0.6f * t, 0f);
                SetAlpha(1f - t);
            });
            SetAlpha(0f);
            _renderer.enabled = false;
        }

        void SetAlpha(float a)
        {
            var c = _renderer.color; c.a = a; _renderer.color = c;
        }

        static float EaseOut(float t) => 1f - (1f - t) * (1f - t);

        static IEnumerator Tween(float dur, System.Action<float> step)
        {
            float e = 0f;
            while (e < dur) { e += Time.deltaTime; step(Mathf.Clamp01(e / dur)); yield return null; }
            step(1f);
        }
    }
}
