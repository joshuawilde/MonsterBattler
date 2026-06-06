using System;
using MonsterBattler.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterBattler.Game.UI
{
    /// <summary>
    /// One PS-style switch button on the bench row. Shows species name + HP bar; tap to swap.
    /// </summary>
    public sealed class SwitchButton : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] Image _background;
        [SerializeField] Text _nameText;
        [SerializeField] Image _hpFill;

        public event Action Clicked;

        void Awake()
        {
            if (_button != null) _button.onClick.AddListener(() => Clicked?.Invoke());
        }

        public void Show(Pokemon mon, bool isActive)
        {
            if (mon == null) { gameObject.SetActive(false); return; }
            gameObject.SetActive(true);
            if (_nameText != null) _nameText.text = mon.Species?.Name ?? mon.Nickname ?? "?";
            float frac = mon.MaxStats[(int)Stat.HP] == 0 ? 0f : (float)mon.CurrentHp / mon.MaxStats[(int)Stat.HP];
            if (_hpFill != null) _hpFill.fillAmount = frac;
            if (_background != null)
            {
                if (mon.IsFainted)        _background.color = new Color(0.35f, 0.10f, 0.10f, 0.95f);
                else if (isActive)        _background.color = new Color(0.20f, 0.40f, 0.25f, 0.95f);
                else                      _background.color = new Color(0.18f, 0.18f, 0.22f, 0.90f);
            }
            // Always tappable — every portrait opens the info panel; whether an actual swap is legal
            // is decided there. Color (above) conveys fainted/active state.
            if (_button != null) _button.interactable = true;
        }
    }
}
