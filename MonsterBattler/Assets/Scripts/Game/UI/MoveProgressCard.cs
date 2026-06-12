using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterBattler.Game.UI
{
    /// <summary>
    /// One post-battle move-progress reveal: the card slides in from the left, its progress bar
    /// fills from the old value to the new one, and if it reaches 100% the bar flips gold, a
    /// "MOVE UNLOCKED!" label pops, a white flash blooms, and the spark burst fires.
    /// Driven by <see cref="Play"/> (BattleView yields it per gain, so cards reveal one by one).
    /// </summary>
    public sealed class MoveProgressCard : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _moveName;
        [SerializeField] TextMeshProUGUI _monName;
        [SerializeField] Image _monIcon;            // the mon's sprite (left edge)
        [SerializeField] Image _barFill;
        [SerializeField] TextMeshProUGUI _barLabel;     // "3/10"
        [SerializeField] GameObject _unlockedLabel;     // "MOVE UNLOCKED!" (inactive by default)
        [SerializeField] Image _flash;                  // soft white bloom (alpha 0 by default)
        [SerializeField] UnlockBurst _burst;
        [SerializeField] CanvasGroup _group;

        static readonly Color BarBlue = new(0.36f, 0.66f, 1f);
        static readonly Color BarGold = new(1f, 0.82f, 0.30f);

        const float SlideDur = 0.32f, FillDur = 0.55f, FlashDur = 0.45f;

        /// <summary>Set the mon sprite shown on the card's left edge (hidden when null).</summary>
        public void SetMonIcon(Sprite s)
        {
            if (_monIcon == null) return;
            _monIcon.sprite = s;
            _monIcon.enabled = s != null;
        }

        /// <summary>Run the full reveal. from/to are 0..1 bar fractions; label shows pts/cost.
        /// <paramref name="unlockedText"/> overrides the burst label (e.g. "LEVEL UP!").</summary>
        public IEnumerator Play(string moveName, string monName, float from, float to, int pts, int cost, bool unlocked,
                                string unlockedText = null)
        {
            if (_moveName != null) _moveName.text = moveName;
            if (_monName != null) _monName.text = monName;
            if (_unlockedLabel != null && unlockedText != null)
            {
                var label = _unlockedLabel.GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
                if (label != null) label.text = unlockedText;
            }
            if (_barLabel != null) _barLabel.text = $"{Mathf.Min(pts, cost)}/{cost}";
            if (_barFill != null) { _barFill.fillAmount = from; _barFill.color = BarBlue; }
            if (_unlockedLabel != null) _unlockedLabel.SetActive(false);
            if (_flash != null) _flash.color = new Color(1f, 1f, 1f, 0f);

            var rt = (RectTransform)transform;
            float homeX = rt.anchoredPosition.x;

            // 1. slide in from off-screen left (ease-out-cubic), fading up
            rt.anchoredPosition = new Vector2(homeX - 900f, rt.anchoredPosition.y);
            if (_group != null) _group.alpha = 0f;
            for (float t = 0f; t < SlideDur; t += Time.unscaledDeltaTime)
            {
                float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / SlideDur), 3f);
                rt.anchoredPosition = new Vector2(Mathf.Lerp(homeX - 900f, homeX, k), rt.anchoredPosition.y);
                if (_group != null) _group.alpha = k;
                yield return null;
            }
            rt.anchoredPosition = new Vector2(homeX, rt.anchoredPosition.y);
            if (_group != null) _group.alpha = 1f;
            yield return Wait(0.12f);

            // 2. bar fills old → new
            for (float t = 0f; t < FillDur; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / FillDur));
                if (_barFill != null) _barFill.fillAmount = Mathf.Lerp(from, to, k);
                yield return null;
            }
            if (_barFill != null) _barFill.fillAmount = to;

            // 3. unlocked: gold bar + spark burst + bloom + label pop
            if (unlocked)
            {
                if (_barFill != null) _barFill.color = BarGold;
                if (_burst != null) _burst.Play();
                if (_unlockedLabel != null) _unlockedLabel.SetActive(true);
                var lrt = _unlockedLabel != null ? _unlockedLabel.transform : null;
                for (float t = 0f; t < FlashDur; t += Time.unscaledDeltaTime)
                {
                    float k = Mathf.Clamp01(t / FlashDur);
                    if (_flash != null)
                    {
                        _flash.color = new Color(1f, 1f, 1f, 0.85f * (1f - k));
                        _flash.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 2.4f, k);
                    }
                    if (lrt != null)                          // ease-out-back pop
                    {
                        float s = 1f + 2.4f * Mathf.Pow(1f - k, 2f) * Mathf.Sin(k * Mathf.PI);
                        lrt.localScale = Vector3.one * Mathf.Lerp(0.4f, 1f, k) * (0.8f + 0.2f * s);
                    }
                    yield return null;
                }
                if (_flash != null) _flash.color = new Color(1f, 1f, 1f, 0f);
                if (lrt != null) lrt.localScale = Vector3.one;
                yield return Wait(0.25f);                     // let the sparks breathe
            }
            else yield return Wait(0.10f);
        }

        static WaitForSecondsRealtime Wait(float s) => new(s);
    }
}
