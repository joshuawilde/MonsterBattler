using System;
using System.Collections.Generic;
using MonsterBattler.Sim;
using MonsterBattler.Sim.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterBattler.Game.UI
{
    /// <summary>
    /// One move button: a pale type-tinted card with the bold move name, a power/accuracy +
    /// description blurb, and the type (bottom-left) + PP (bottom-right, tinted by remaining).
    /// </summary>
    public sealed class MoveButton : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] Image _background;
        [SerializeField] TextMeshProUGUI _nameText;
        [SerializeField] TextMeshProUGUI _descText;
        [SerializeField] TextMeshProUGUI _typeText;
        [SerializeField] TextMeshProUGUI _ppText;
        [SerializeField] Image _categoryIcon;   // physical / special / status

        static readonly Color Ink = new Color(0.13f, 0.13f, 0.15f);

        public event Action Clicked;

        void Awake()
        {
            if (_button != null) _button.onClick.AddListener(() => Clicked?.Invoke());
        }

        public void Show(MoveSlot slot, Pokemon defender = null)
        {
            if (slot == null || slot.Move == null) { gameObject.SetActive(false); return; }
            gameObject.SetActive(true);
            var move = slot.Move;

            // Type effectiveness vs the current opponent (damaging moves only).
            float eff = -1f;
            if (defender != null && move.Category != MoveCategory.Status && move.BasePower > 0 && defender.Species != null)
            {
                var (d1, d2) = defender.CurrentTypes();
                eff = TypeChart.Effectiveness(move.Type, d1, d2);
            }
            (string effLabel, Color effColor) = EffInfo(eff);

            if (_nameText != null) { _nameText.text = move.Name; _nameText.color = Ink; }
            if (_descText != null)
            {
                string blurb = DescBlurb(move);
                if (effLabel != null)
                {
                    string tag = $"<b><color=#{ColorUtility.ToHtmlStringRGB(effColor)}>{effLabel}</color></b>";
                    blurb = string.IsNullOrEmpty(blurb) ? tag : $"{tag}\n{blurb}";
                }
                _descText.text = blurb; _descText.color = Ink;
            }
            if (_typeText != null) { _typeText.text = TypeStyle.Display(move.Type); _typeText.color = Ink; }
            CategoryIcons.Apply(_categoryIcon, move.Category);
            if (_ppText != null)
            {
                _ppText.text = $"{slot.Pp}/{slot.MaxPp}";
                float r = slot.MaxPp > 0 ? (float)slot.Pp / slot.MaxPp : 0f;
                _ppText.color = r > 0.5f ? new Color(0.20f, 0.50f, 0.22f)
                              : r > 0.25f ? new Color(0.70f, 0.45f, 0.10f)
                                          : new Color(0.72f, 0.20f, 0.20f);
            }
            if (_background != null)
            {
                var bg = Color.Lerp(TypeStyle.BgColor(move.Type), Color.white, 0.62f);
                var green = new Color(0.45f, 0.85f, 0.45f);
                var red = new Color(0.90f, 0.50f, 0.42f);
                if (eff >= 4f) bg = Color.Lerp(bg, green, 0.62f);                 // extremely effective
                else if (eff > 1f) bg = Color.Lerp(bg, green, 0.42f);            // super effective
                else if (eff == 0f) bg = Color.Lerp(bg, new Color(0.55f, 0.55f, 0.60f), 0.55f); // no effect
                else if (eff > 0f && eff <= 0.25f) bg = Color.Lerp(bg, red, 0.58f); // mostly ineffective
                else if (eff >= 0f && eff < 1f) bg = Color.Lerp(bg, red, 0.36f);    // not very effective
                _background.color = bg;
            }
        }

        // Effectiveness → (label, color). Returns (null, _) for neutral (×1) / non-damaging / status.
        static (string, Color) EffInfo(float eff)
        {
            if (eff < 0f || eff == 1f) return (null, default);
            if (eff >= 4f)   return ($"Extremely effective ×{Mult(eff)}",  new Color(0.10f, 0.70f, 0.30f)); // ×4
            if (eff > 1f)    return ($"Super effective ×{Mult(eff)}",      new Color(0.20f, 0.58f, 0.24f)); // ×2
            if (eff == 0f)   return ("No effect",                          new Color(0.45f, 0.45f, 0.50f)); // ×0
            if (eff <= 0.25f) return ($"Mostly ineffective ×{Mult(eff)}",  new Color(0.72f, 0.18f, 0.18f)); // ×¼
            return ($"Not very effective ×{Mult(eff)}",                    new Color(0.85f, 0.42f, 0.22f)); // ×½
        }

        static string Mult(float e) => e == 0.25f ? "¼" : e == 0.5f ? "½" : e == 2f ? "2" : e == 4f ? "4" : e.ToString("0.##");

        // "90 BP · 100%" header (omitting power for status, accuracy for never-miss) + description.
        static string DescBlurb(MoveData m)
        {
            var stats = new List<string>();
            if (m.BasePower > 0) stats.Add($"{m.BasePower} BP");
            if (m.Accuracy > 0) stats.Add($"{m.Accuracy}%");
            string header = string.Join(" · ", stats);
            string desc = m.ShortDesc ?? "";
            return header.Length > 0 ? $"{header}\n{desc}" : desc;
        }

        CanvasGroup _group;

        public void SetInteractable(bool on)
        {
            if (_button != null) _button.interactable = on;
            // When you can't act (opponent's beat, forced switch, choice-locked) the card fades
            // way down so the actionable state is unmistakable.
            // NOTE: no ?? on GetComponent — Unity's fake-null breaks it (MissingComponentException).
            if (_group == null && !TryGetComponent(out _group)) _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = on ? 1f : 0.2f;
        }
    }
}
