using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterBattler.Game.Meta
{
    /// <summary>One row in the move-equip list: move name (top-left), a stats sub line (top-right:
    /// power·type, or unlock progress for locked moves), and a description of what the move does
    /// (bottom, full width). Tinted by state: equipped / unlocked / locked.</summary>
    public sealed class MoveCell : MonoBehaviour
    {
        [SerializeField] Image _bg;
        [SerializeField] TextMeshProUGUI _name;
        [SerializeField] TextMeshProUGUI _sub;
        [SerializeField] TextMeshProUGUI _desc;  // what the move does
        [SerializeField] Button _button;
        [SerializeField] GameObject _progressRoot; // unlock progress bar (locked moves only)
        [SerializeField] Image _progressFill;      // filled-horizontal Image inside it
        [SerializeField] Image _catIcon;           // physical / special / status icon
        [SerializeField] Image _typeStrip;         // type-art accent fading out along the left edge

        public string Id { get; private set; }
        public event Action<string> Clicked;

        void Awake()
        {
            if (_button != null) _button.onClick.AddListener(() => Clicked?.Invoke(Id));
        }

        /// <param name="progress">Unlock progress 0..1 for locked moves; pass -1 to hide the bar.</param>
        public void Show(string id, string name, string sub, string desc, Color bg, Color nameColor, bool tappable,
                         float progress = -1f, Sim.MoveCategory? category = null, Sim.MonType? moveType = null)
        {
            Id = id;
            if (category.HasValue) UI.CategoryIcons.Apply(_catIcon, category.Value);
            else if (_catIcon != null) _catIcon.enabled = false;
            if (_typeStrip != null)
            {
                if (moveType.HasValue && moveType.Value != Sim.MonType.None)
                {
                    _typeStrip.sprite = UI.TypeBgSprites.GetStrip(moveType.Value);
                    _typeStrip.enabled = true;
                    _typeStrip.color = Color.white;
                }
                else _typeStrip.enabled = false;
            }
            if (_name != null) { _name.text = name; _name.color = nameColor; }
            if (_sub != null) _sub.text = sub;
            if (_desc != null) _desc.text = desc ?? "";
            if (_bg != null) _bg.color = bg;
            if (_button != null) _button.interactable = tappable;
            if (_progressRoot != null) _progressRoot.SetActive(progress >= 0f);
            if (_progressFill != null && progress >= 0f)
            {
                // Width by anchors (not Image.fillAmount — unreliable on sprite-less Filled images).
                var rt = _progressFill.rectTransform;
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }
    }
}
