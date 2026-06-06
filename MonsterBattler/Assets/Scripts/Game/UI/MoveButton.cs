using System;
using System.Collections.Generic;
using MonsterBattler.Sim;
using MonsterBattler.Sim.Data;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterBattler.Game.UI
{
    /// <summary>
    /// One PS-style move button: type-tinted background, move name, type pill, PP counter, and a
    /// power/accuracy + description blurb. All child elements are authored in the scene; this script
    /// only configures them.
    /// </summary>
    public sealed class MoveButton : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] Image _background;
        [SerializeField] Text _nameText;
        [SerializeField] Text _typeText;
        [SerializeField] Text _ppText;
        [SerializeField] Text _descText;   // power/accuracy + description

        /// <summary>Fired when the player taps this button.</summary>
        public event Action Clicked;

        void Awake()
        {
            if (_button != null) _button.onClick.AddListener(() => Clicked?.Invoke());
        }

        public void Show(MoveSlot slot)
        {
            if (slot == null || slot.Move == null) { gameObject.SetActive(false); return; }
            gameObject.SetActive(true);
            if (_nameText != null) _nameText.text = slot.Move.Name;
            if (_typeText != null) _typeText.text = TypeStyle.Display(slot.Move.Type);
            if (_ppText   != null) _ppText.text   = $"{slot.Pp}/{slot.MaxPp}";
            if (_descText != null) _descText.text = DescBlurb(slot.Move);
            if (_background != null) _background.color = TypeStyle.BgColor(slot.Move.Type);
        }

        // "90 BP · 100%" header (omitting power for status moves and accuracy for never-miss moves)
        // followed by the move's description on the next line.
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
