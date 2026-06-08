using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterBattler.Game.Meta
{
    /// <summary>One monster cell in the collection/box grid: thumbnail + name + selected outline + tap.</summary>
    public sealed class MonCell : MonoBehaviour
    {
        [SerializeField] Image _thumb;
        [SerializeField] TextMeshProUGUI _name;
        [SerializeField] GameObject _selectedOutline;
        [SerializeField] Button _button;
        [SerializeField] Image _cardBg; // tinted by rarity

        public event Action<string> Clicked;
        public string Id { get; private set; }

        void Awake()
        {
            if (_button != null) _button.onClick.AddListener(() => Clicked?.Invoke(Id));
        }

        public void Show(string speciesId, string displayName, bool selected, Color cardColor)
        {
            Id = speciesId;
            if (_cardBg != null) _cardBg.color = cardColor;
            if (_thumb != null)
            {
                var s = MonSpriteLoader.Load(speciesId, back: false);
                _thumb.sprite = s;
                _thumb.enabled = s != null;
            }
            if (_name != null) _name.text = displayName;
            SetSelected(selected);
        }

        public void SetSelected(bool selected)
        {
            if (_selectedOutline != null) _selectedOutline.SetActive(selected);
        }
    }
}
