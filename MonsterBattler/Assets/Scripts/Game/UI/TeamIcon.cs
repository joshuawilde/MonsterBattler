using System;
using MonsterBattler.Sim;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterBattler.Game.UI
{
    /// <summary>
    /// One team-roster slot, used for BOTH sides (same prefab). Shows the mon's thumbnail, name, an
    /// HP bar, and 3-letter type tags. Unseen enemy mons are hidden behind a ghost; the player's own
    /// not-yet-sent-out mons get a small corner ghost badge. Dims when fainted, outlines the active mon,
    /// and raises <see cref="Clicked"/> to open its info panel.
    /// </summary>
    public sealed class TeamIcon : MonoBehaviour
    {
        [SerializeField] Image _thumbnail;
        [SerializeField] Image _hpFill;
        [SerializeField] Image _background;
        [SerializeField] Outline _activeOutline;
        [SerializeField] Button _button;
        [SerializeField] TextMeshProUGUI _nameText;
        [SerializeField] Image _typePip1;
        [SerializeField] Image _typePip2;
        [SerializeField] TextMeshProUGUI _typeText1;   // 3-letter tag over pip 1
        [SerializeField] TextMeshProUGUI _typeText2;   // 3-letter tag over pip 2
        [SerializeField] GameObject _ghostOverlay;     // covers thumbnail for an unseen enemy
        [SerializeField] GameObject _unplayedBadge;    // corner badge for an own mon not yet sent out

        public event Action Clicked;

        void Awake()
        {
            if (_button != null) _button.onClick.AddListener(() => Clicked?.Invoke());
        }

        public void Show(Pokemon mon, bool isActive, bool isEnemy = false)
        {
            if (mon == null || mon.Species == null) { gameObject.SetActive(false); return; }
            gameObject.SetActive(true);

            bool seen = mon.HasBeenActive || isActive;
            bool hidden = isEnemy && !seen;           // unseen enemy → ghost
            bool unplayed = !isEnemy && !seen;        // own mon not yet sent out → corner badge

            if (_ghostOverlay != null) _ghostOverlay.SetActive(hidden);
            if (_unplayedBadge != null) _unplayedBadge.SetActive(unplayed);

            if (_thumbnail != null)
            {
                if (hidden) { _thumbnail.enabled = false; }
                else
                {
                    var sp = MonSpriteLoader.Load(mon.Species.Id, back: false);
                    _thumbnail.sprite = sp;
                    _thumbnail.enabled = sp != null;
                    _thumbnail.color = mon.IsFainted ? new Color(1f, 1f, 1f, 0.30f) : Color.white;
                }
            }

            if (_nameText != null)
            {
                _nameText.text = hidden ? "???" : mon.Species.Name;
                _nameText.color = mon.IsFainted ? new Color(0.7f, 0.7f, 0.7f) : Color.white;
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

            // Type tags (hidden for an unseen enemy).
            float pa = mon.IsFainted ? 0.4f : 1f;
            SetPip(_typePip1, _typeText1, hidden ? MonType.None : mon.Species.Type1, pa);
            var t2 = mon.Species.Type2;
            bool has2 = !hidden && t2 != MonType.None && t2 != mon.Species.Type1;
            SetPip(_typePip2, _typeText2, has2 ? t2 : MonType.None, pa);
        }

        static void SetPip(Image pip, TextMeshProUGUI label, MonType type, float alpha)
        {
            bool show = type != MonType.None;
            if (pip != null)
            {
                if (show) { var c = TypeStyle.BgColor(type); c.a = alpha; pip.color = c; }
                pip.enabled = show;
            }
            if (label != null)
            {
                label.text = show ? TypeStyle.Abbrev(type) : "";
                label.enabled = show;
            }
        }

        static Color HpColor(float f) =>
            f > 0.5f ? new Color(0.30f, 0.78f, 0.33f)
          : f > 0.2f ? new Color(0.95f, 0.78f, 0.20f)
                     : new Color(0.86f, 0.27f, 0.24f);
    }
}
