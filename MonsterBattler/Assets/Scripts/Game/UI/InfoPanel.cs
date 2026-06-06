using System;
using MonsterBattler.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterBattler.Game.UI
{
    /// <summary>
    /// On-demand details overlay for a Pokemon (stats, ability, item, type matchup, move
    /// descriptions). Carries its own Close button, and a Swap button shown only when the inspected
    /// mon is a legal switch target. BattleView feeds it the mon and handles the actions; the
    /// formatting lives in <see cref="PokemonInfoText"/>.
    /// </summary>
    public sealed class InfoPanel : MonoBehaviour
    {
        [SerializeField] Text _text;
        [SerializeField] Button _closeButton;
        [SerializeField] Button _swapButton;

        public event Action CloseRequested;
        public event Action SwapRequested;

        public bool IsVisible => gameObject.activeSelf;

        void Awake()
        {
            if (_closeButton != null) _closeButton.onClick.AddListener(() => CloseRequested?.Invoke());
            if (_swapButton != null) _swapButton.onClick.AddListener(() => SwapRequested?.Invoke());
        }

        public void Show(Pokemon mon, bool canSwap)
        {
            if (_text != null) _text.text = PokemonInfoText.Build(mon);
            if (_swapButton != null) _swapButton.gameObject.SetActive(canSwap);
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);
    }
}
