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
        [SerializeField] GameObject _selectedOutline; // yellow frame = the cell whose preview is open
        [SerializeField] Button _button;
        [SerializeField] Image _cardBg;     // tinted by rarity
        [SerializeField] GameObject _teamBadge; // small "TEAM" chip = on the battle team
        [SerializeField] Image _typeBg;     // type-colored plate behind the thumbnail (diagonal for duals)

        public event Action<string> Clicked;
        public string Id { get; private set; }

        void Awake()
        {
            if (_button != null) _button.onClick.AddListener(() => Clicked?.Invoke(Id));
        }

        public void Show(string speciesId, string displayName, bool inTeam, Color rarityColor)
        {
            Id = speciesId;
            // The full-cell backdrop is the TYPE plate; rarity lives in the name color.
            if (_cardBg != null) _cardBg.color = new Color(0.10f, 0.11f, 0.15f, 0.95f);
            var dex = MetaGame.Dex;
            UI.TypeBgSprites.Apply(_typeBg, dex != null && dex.Species.TryGetValue(speciesId, out var sp) ? sp : null);
            if (_thumb != null)
            {
                var s = MonSpriteLoader.Load(speciesId, back: false);
                _thumb.sprite = s;
                _thumb.enabled = s != null;
            }
            if (_name != null) { _name.text = displayName; _name.color = rarityColor; }
            SetInTeam(inTeam);
            SetSelected(false);
        }

        /// <summary>Yellow frame: this cell's preview is the one open below.</summary>
        public void SetSelected(bool selected)
        {
            if (_selectedOutline != null) _selectedOutline.SetActive(selected);
        }

        /// <summary>Green corner chip: this mon is on the battle team.</summary>
        public void SetInTeam(bool inTeam)
        {
            if (_teamBadge != null) _teamBadge.SetActive(inTeam);
        }
    }
}
