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

            // Light type over the baked scrim, with dark outlines (GPT-recommended contrast recipe).
            var titleCol = new Color(0.96f, 0.97f, 0.98f);
            var bodyCol = new Color(0.90f, 0.92f, 0.95f);
            if (_nameText != null) { _nameText.text = move.Name; _nameText.color = titleCol; StyleOutline(_nameText, 0.22f); }
            if (_descText != null)
            {
                string blurb = DescBlurb(move);
                if (effLabel != null)
                {
                    string tag = $"<b><color=#{ColorUtility.ToHtmlStringRGB(effColor)}>{effLabel}</color></b>";
                    blurb = string.IsNullOrEmpty(blurb) ? tag : $"{tag}\n{blurb}";
                }
                _descText.text = blurb; _descText.color = bodyCol; StyleOutline(_descText, 0.16f);
            }
            if (_typeText != null) { _typeText.text = TypeStyle.Display(move.Type); _typeText.color = bodyCol; StyleOutline(_typeText, 0.16f); }
            CategoryIcons.Apply(_categoryIcon, move.Category);
            if (_ppText != null)
            {
                _ppText.text = $"{slot.Pp}/{slot.MaxPp}";
                float r = slot.MaxPp > 0 ? (float)slot.Pp / slot.MaxPp : 0f;
                _ppText.color = r > 0.5f ? new Color(0.45f, 0.90f, 0.50f)
                              : r > 0.25f ? new Color(1f, 0.78f, 0.30f)
                                          : new Color(1f, 0.45f, 0.40f);
                StyleOutline(_ppText, 0.16f);
            }
            if (_background != null)
            {
                // Card face = pale wash of the type ART; effectiveness tints it multiplicatively.
                _background.sprite = TypeBgSprites.GetPale(move.Type);
                var tint = Color.white;
                var green = new Color(0.62f, 1f, 0.62f);
                var red = new Color(1f, 0.62f, 0.55f);
                if (eff >= 4f) tint = Color.Lerp(Color.white, green, 0.85f);     // extremely effective
                else if (eff > 1f) tint = Color.Lerp(Color.white, green, 0.55f); // super effective
                else if (eff == 0f) tint = new Color(0.62f, 0.62f, 0.66f);       // no effect
                else if (eff > 0f && eff <= 0.25f) tint = Color.Lerp(Color.white, red, 0.78f); // mostly ineffective
                else if (eff >= 0f && eff < 1f) tint = Color.Lerp(Color.white, red, 0.48f);    // not very effective
                _background.color = tint;
            }
        }

        // TMP outline on a per-instance material (TryGetComponent-safe; fontMaterial instantiates once).
        static void StyleOutline(TextMeshProUGUI t, float width)
        {
            if (t == null) return;
            t.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, width);
            t.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 0.8f));
        }

        // Effectiveness → (label, color). Bright variants — they sit on the dark scrim now.
        static (string, Color) EffInfo(float eff)
        {
            if (eff < 0f || eff == 1f) return (null, default);
            if (eff >= 4f)   return ($"Extremely effective ×{Mult(eff)}",  new Color(0.35f, 1f, 0.50f));    // ×4
            if (eff > 1f)    return ($"Super effective ×{Mult(eff)}",      new Color(0.45f, 0.95f, 0.50f)); // ×2
            if (eff == 0f)   return ("No effect",                          new Color(0.72f, 0.72f, 0.78f)); // ×0
            if (eff <= 0.25f) return ($"Mostly ineffective ×{Mult(eff)}",  new Color(1f, 0.42f, 0.38f));    // ×¼
            return ($"Not very effective ×{Mult(eff)}",                    new Color(1f, 0.62f, 0.30f));    // ×½
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
