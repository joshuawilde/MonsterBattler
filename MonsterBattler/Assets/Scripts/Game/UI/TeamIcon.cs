using System;
using MonsterBattler.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterBattler.Game.UI
{
    /// <summary>
    /// One team-roster slot, used for BOTH sides (same prefab). Shows the mon's thumbnail + an HP bar,
    /// dims when fainted, outlines the active mon, and raises <see cref="Clicked"/> to open its info
    /// panel (the panel decides whether a swap is legal).
    /// </summary>
    public sealed class TeamIcon : MonoBehaviour
    {
        [SerializeField] Image _thumbnail;
        [SerializeField] Image _hpFill;
        [SerializeField] Image _background;
        [SerializeField] Outline _activeOutline; // optional highlight for the active mon
        [SerializeField] Button _button;

        public event Action Clicked;

        void Awake()
        {
            if (_button != null) _button.onClick.AddListener(() => Clicked?.Invoke());
        }

        public void Show(Pokemon mon, bool isActive)
        {
            if (mon == null || mon.Species == null) { gameObject.SetActive(false); return; }
            gameObject.SetActive(true);

            if (_thumbnail != null)
            {
                var sp = MonSpriteLoader.Load(mon.Species.Id, back: false);
                _thumbnail.sprite = sp;
                _thumbnail.enabled = sp != null;
                _thumbnail.color = mon.IsFainted ? new Color(1f, 1f, 1f, 0.30f) : Color.white;
            }
            if (_hpFill != null)
            {
                float frac = mon.MaxStats[(int)Stat.HP] > 0 ? (float)mon.CurrentHp / mon.MaxStats[(int)Stat.HP] : 0f;
                _hpFill.fillAmount = frac;
                _hpFill.color = HpColor(frac);
            }
            if (_background != null)
                _background.color = mon.IsFainted ? new Color(0.25f, 0.10f, 0.10f, 0.92f)
                                  : isActive       ? new Color(0.20f, 0.40f, 0.26f, 0.95f)
                                                   : new Color(0.13f, 0.14f, 0.18f, 0.92f);
            if (_activeOutline != null) _activeOutline.enabled = isActive && !mon.IsFainted;
        }

        static Color HpColor(float f) =>
            f > 0.5f ? new Color(0.30f, 0.78f, 0.33f)
          : f > 0.2f ? new Color(0.95f, 0.78f, 0.20f)
                     : new Color(0.86f, 0.27f, 0.24f);
    }
}
