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

        static readonly Color Ink = new Color(0.13f, 0.13f, 0.15f);

        public event Action Clicked;

        void Awake()
        {
            if (_button != null) _button.onClick.AddListener(() => Clicked?.Invoke());
        }

        public void Show(MoveSlot slot)
        {
            if (slot == null || slot.Move == null) { gameObject.SetActive(false); return; }
            gameObject.SetActive(true);
            var move = slot.Move;

            if (_nameText != null) { _nameText.text = move.Name; _nameText.color = Ink; }
            if (_descText != null) { _descText.text = DescBlurb(move); _descText.color = Ink; }
            if (_typeText != null) { _typeText.text = TypeStyle.Display(move.Type); _typeText.color = Ink; }
            if (_ppText != null)
            {
                _ppText.text = $"{slot.Pp}/{slot.MaxPp}";
                float r = slot.MaxPp > 0 ? (float)slot.Pp / slot.MaxPp : 0f;
                _ppText.color = r > 0.5f ? new Color(0.20f, 0.50f, 0.22f)
                              : r > 0.25f ? new Color(0.70f, 0.45f, 0.10f)
                                          : new Color(0.72f, 0.20f, 0.20f);
            }
            if (_background != null) _background.color = Color.Lerp(TypeStyle.BgColor(move.Type), Color.white, 0.62f);
        }

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

        public void SetInteractable(bool on)
        {
            if (_button != null) _button.interactable = on;
        }
    }
}
