using System;
using System.Collections.Generic;
using MonsterBattler.Sim;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterBattler.Game.UI
{
    /// <summary>
    /// Details overlay for a Pokemon. Header + body are TMP text; the mon's types and the defensive
    /// matchup (x4/x2/x½/x¼/x0) are real <see cref="TypeBadge"/> chips laid out in HLG rows. Carries
    /// its own Close button and a Swap button (shown only when the mon is a legal switch target).
    /// </summary>
    public sealed class InfoPanel : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _header;
        [SerializeField] TextMeshProUGUI _topBody;    // hp% + stats (above the matchup)
        [SerializeField] TextMeshProUGUI _bottomBody; // ability + item + moves (below the matchup)
        [SerializeField] Transform _typesRow;        // HLG: the mon's type chips
        [SerializeField] Transform _matchupParent;   // holds the 5 EffRow children (x4..x0)
        [SerializeField] TypeBadge _typeBadgePrefab; // chip instantiated per type
        [SerializeField] Button _closeButton;
        [SerializeField] Button _swapButton;

        static readonly float[] Buckets = { 4f, 2f, 0.5f, 0.25f, 0f };
        static readonly string[] BucketLabels = { "x4", "x2", "x½", "x¼", "x0" };

        EffRow[] _effRows;

        public event Action CloseRequested;
        public event Action SwapRequested;

        public bool IsVisible => gameObject.activeSelf;

        void Awake()
        {
            if (_closeButton != null) _closeButton.onClick.AddListener(() => CloseRequested?.Invoke());
            if (_swapButton != null) _swapButton.onClick.AddListener(() => SwapRequested?.Invoke());
            if (_matchupParent != null) _effRows = _matchupParent.GetComponentsInChildren<EffRow>(includeInactive: true);
        }

        public void Show(Pokemon mon, bool canSwap)
        {
            if (mon == null || mon.Species == null) return;
            if (_header != null) _header.text = PokemonInfoText.HeaderText(mon);
            if (_topBody != null) _topBody.text = PokemonInfoText.TopBodyText(mon);
            if (_bottomBody != null) _bottomBody.text = PokemonInfoText.BottomBodyText(mon);

            // Type chips.
            if (_typesRow != null)
            {
                ClearChildren(_typesRow);
                foreach (var t in PokemonInfoText.EffectiveTypes(mon)) MakeBadge(_typesRow).SetType(t);
            }

            // Defensive matchup rows.
            if (_effRows != null)
            {
                var t1 = mon.IsTerastallized ? mon.TeraType : mon.Species.Type1;
                var t2 = mon.IsTerastallized ? MonType.None : mon.Species.Type2;
                var matchup = TypeMatchup.Defensive(t1, t2);
                for (int i = 0; i < _effRows.Length && i < Buckets.Length; i++)
                {
                    var row = _effRows[i];
                    var types = new List<MonType>();
                    foreach (var e in matchup) if (e.Multiplier == Buckets[i]) types.Add(e.Type);
                    bool any = types.Count > 0;
                    row.gameObject.SetActive(any);
                    if (!any) continue;
                    row.SetLabel(BucketLabels[i]);
                    row.ClearBadges();
                    foreach (var t in types) MakeBadge(row.transform).SetType(t);
                }
            }

            if (_swapButton != null) _swapButton.gameObject.SetActive(canSwap);
        }

        TypeBadge MakeBadge(Transform parent) => Instantiate(_typeBadgePrefab, parent);

        static void ClearChildren(Transform row)
        {
            for (int i = row.childCount - 1; i >= 0; i--) Destroy(row.GetChild(i).gameObject);
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);
    }
}
