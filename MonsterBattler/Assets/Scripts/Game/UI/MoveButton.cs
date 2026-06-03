using System;
using MonsterBattler.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterBattler.Game.UI
{
    /// <summary>
    /// One PS-style move button: type-tinted background, move name, type pill, PP counter.
    /// All child elements are authored in the scene; this script only configures them.
    /// </summary>
    public sealed class MoveButton : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] Image _background;
        [SerializeField] Text _nameText;
        [SerializeField] Text _typeText;
        [SerializeField] Text _ppText;

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
            if (_background != null) _background.color = TypeStyle.BgColor(slot.Move.Type);
        }

        public void SetInteractable(bool on)
        {
            if (_button != null) _button.interactable = on;
        }
    }
}
