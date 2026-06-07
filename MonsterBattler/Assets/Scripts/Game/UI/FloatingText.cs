using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterBattler.Game.UI
{
    /// <summary>
    /// A Showdown-style combat popup: a small colored chip that floats up from a mon and fades out,
    /// then destroys itself. Spawned over the active mon for damage/heal/stat/status/miss/ability.
    /// </summary>
    public sealed class FloatingText : MonoBehaviour
    {
        [SerializeField] Image _background;
        [SerializeField] TextMeshProUGUI _label;
        [SerializeField] CanvasGroup _group;

        const float Life = 1.1f;   // seconds
        const float Rise = 110f;   // pixels it drifts up over its life

        float _t;
        Vector2 _start;

        /// <summary>Set the text + background color and start the float/fade.</summary>
        public void Show(string text, Color bg)
        {
            if (_label != null) _label.text = text;
            if (_background != null) _background.color = bg;
            _start = ((RectTransform)transform).anchoredPosition;
            _t = 0f;
        }

        void Update()
        {
            _t += Time.deltaTime;
            float f = _t / Life;
            ((RectTransform)transform).anchoredPosition = _start + Vector2.up * (Rise * f);
            if (_group != null) _group.alpha = f < 0.55f ? 1f : Mathf.Clamp01(1f - (f - 0.55f) / 0.45f);
            if (_t >= Life) Destroy(gameObject);
        }
    }
}
