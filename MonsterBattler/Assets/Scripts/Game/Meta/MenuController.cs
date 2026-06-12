using System.Collections.Generic;
using System.Linq;
using MonsterBattler.Sim.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterBattler.Game.Meta
{
    /// <summary>
    /// Drives the meta loop UI overlaid on the battle scene: Home / Box (team builder) / Summon.
    /// The opaque menu root covers the battle until [Battle] is pressed, which hides it and calls
    /// <see cref="BattleView.BeginBattle"/>. Returning from a battle reloads the scene → Home again.
    /// </summary>
    public sealed class MenuController : MonoBehaviour
    {
        [Header("Roots / panels")]
        [SerializeField] GameObject _menuRoot;
        [SerializeField] GameObject _homePanel;
        [SerializeField] GameObject _boxPanel;
        [SerializeField] GameObject _summonPanel;

        [Header("Home")]
        [SerializeField] TextMeshProUGUI _homeCoins;
        [SerializeField] TextMeshProUGUI _homeTeam;
        [SerializeField] TextMeshProUGUI _homeElo;       // "You · Elo 1000"
        [SerializeField] Button _battleButton;
        [SerializeField] Button _boxButton;
        [SerializeField] Button _summonButton;

        [Header("Matchmaking (scene-authored panel)")]
        [SerializeField] GameObject _matchmakingPanel;
        [SerializeField] TextMeshProUGUI _mmStatus;      // "Searching for opponent…" / "Match found!"
        [SerializeField] TextMeshProUGUI _mmPlayerName;
        [SerializeField] TextMeshProUGUI _mmPlayerElo;
        [SerializeField] TextMeshProUGUI _mmOppName;
        [SerializeField] TextMeshProUGUI _mmOppElo;
        [Tooltip("Seconds to 'search' before a match is found.")]
        [SerializeField] float _searchTime = 1.4f;
        [Tooltip("Seconds the 'Match found' card shows before the battle starts.")]
        [SerializeField] float _foundTime = 1.5f;

        [Header("Box (team builder)")]
        [SerializeField] Transform _boxContent;        // grid parent; cells spawned at runtime
        [SerializeField] MonCell _monCellPrefab;
        [SerializeField] TextMeshProUGUI _boxTeamCount;
        [SerializeField] Button _boxBackButton;

        [Header("Mon detail / move equip (scene-authored)")]
        [SerializeField] GameObject _detailPanel;
        [SerializeField] TextMeshProUGUI _detailTitle;
        [SerializeField] Image _detailImage;
        [SerializeField] Image _detailTypeBg;   // type plate behind the preview sprite
        [SerializeField] Button _detailTeamButton;
        [SerializeField] TextMeshProUGUI _detailTeamLabel;
        [SerializeField] TextMeshProUGUI _detailEquipCount;
        [SerializeField] Transform _detailMoveContent;
        [SerializeField] MoveCell _moveCellPrefab;
        [SerializeField] Button _detailBackButton;

        [Header("Summon")]
        [SerializeField] Button _pullButton;
        [SerializeField] Image _summonImage;
        [SerializeField] Image _summonTypeBg;   // type plate behind the summon result
        [SerializeField] TextMeshProUGUI _summonName;
        [SerializeField] TextMeshProUGUI _summonStatus;
        [SerializeField] TextMeshProUGUI _summonCoins;
        [SerializeField] Button _summonBackButton;

        BattleView _battleView;
        Dex _dex;
        readonly List<MonCell> _cells = new();

        void Awake()
        {
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;        // otherwise vSync caps FPS to refresh and ignores targetFrameRate
            Application.targetFrameRate = 120;
            try { _dex = DexLoader.LoadFromStreamingAssets(); } catch { _dex = null; }
            Wire(_battleButton, OnBattle);
            Wire(_boxButton, ShowBox);
            Wire(_summonButton, ShowSummon);
            Wire(_boxBackButton, ShowHome);
            Wire(_summonBackButton, ShowHome);
            Wire(_pullButton, OnPull);
            Wire(_detailBackButton, ShowBox);
            Wire(_detailTeamButton, OnDetailTeamToggle);
        }

        static void Wire(Button b, UnityEngine.Events.UnityAction a) { if (b != null) b.onClick.AddListener(a); }

        /// <summary>Called by BattleView.Start when a menu is present: show the home screen.</summary>
        public void Boot(BattleView battleView)
        {
            _battleView = battleView;
            if (_menuRoot != null)
            {
                _menuRoot.transform.SetAsLastSibling(); // always render above the battle UI (rosters, etc.)
                _menuRoot.SetActive(true);
            }
            ShowHome();
        }

        string Name(string id) =>
            _dex != null && _dex.Species.TryGetValue(id, out var sp) ? sp.Name : Pretty(id);

        static string Pretty(string id) => string.IsNullOrEmpty(id) ? "?" : char.ToUpper(id[0]) + id.Substring(1);

        // Rarity tier → colors (0 Common, 1 Rare, 2 Epic, 3 Legendary).
        static readonly Color[] RarityText = {
            new Color(0.78f,0.80f,0.85f), new Color(0.36f,0.66f,1f),
            new Color(0.74f,0.45f,1f),    new Color(1f,0.80f,0.25f),
        };
        static Color CardTint(int tier)
        {
            var c = RarityText[Mathf.Clamp(tier, 0, 3)];
            return new Color(c.r * 0.32f, c.g * 0.32f, c.b * 0.34f, 0.95f); // muted card background
        }

        // ---- panel switching ----------------------------------------------------------------
        void ShowHome()
        {
            SetPanels(home: true, box: false, summon: false);
            int teamN = MetaGame.BattleTeam().Count;
            if (_homeCoins != null) _homeCoins.text = $"{MetaGame.Profile.coins} coins";
            if (_homeTeam != null) _homeTeam.text = $"Team {teamN}/{MetaGame.TeamSize}  ·  {MetaGame.Profile.owned.Count} owned";
            if (_homeElo != null) _homeElo.text = $"{MetaGame.Profile.username}  ·  Elo {MetaGame.Profile.elo}";
        }

        void ShowBox()
        {
            _detailId = null; // fresh Box entry: nothing selected, preview hidden
            SetPanels(home: false, box: true, summon: false);
            PopulateBox();
        }

        void ShowSummon()
        {
            SetPanels(home: false, box: false, summon: true);
            if (_summonStatus != null) _summonStatus.text = $"Summon — {MetaGame.PullCost} coins";
            if (_summonName != null) _summonName.text = "";
            if (_summonImage != null) _summonImage.gameObject.SetActive(false);
            if (_summonTypeBg != null) _summonTypeBg.enabled = false;
            RefreshSummonCoins();
        }

        void SetPanels(bool home, bool box, bool summon, bool detail = false)
        {
            if (_homePanel != null) _homePanel.SetActive(home);
            if (_boxPanel != null) _boxPanel.SetActive(box);
            if (_summonPanel != null) _summonPanel.SetActive(summon);
            if (_detailPanel != null) _detailPanel.SetActive(detail);
            if (_matchmakingPanel != null) _matchmakingPanel.SetActive(false);
        }

        // ---- actions ------------------------------------------------------------------------
        void OnBattle()
        {
            if (_battleView == null) _battleView = FindObjectOfType<BattleView>();
            StopAllCoroutines();
            StartCoroutine(MatchmakingFlow());
        }

        // "Searching for opponent…" → "Match found! You vs <opp>" (with Elos) → start the battle.
        System.Collections.IEnumerator MatchmakingFlow()
        {
            if (_homePanel != null) _homePanel.SetActive(false);
            if (_boxPanel != null) _boxPanel.SetActive(false);
            if (_summonPanel != null) _summonPanel.SetActive(false);
            if (_matchmakingPanel != null) _matchmakingPanel.SetActive(true);

            var prof = MetaGame.Profile;
            if (_mmPlayerName != null) _mmPlayerName.text = prof.username;
            if (_mmPlayerElo != null) _mmPlayerElo.text = $"Elo {prof.elo}";
            if (_mmStatus != null) _mmStatus.text = "Searching for opponent…";
            if (_mmOppName != null) _mmOppName.text = "?";
            if (_mmOppElo != null) _mmOppElo.text = "";
            yield return new WaitForSeconds(Mathf.Max(0.1f, _searchTime));

            var opp = MetaGame.StartMatchmaking();
            if (_mmOppName != null) _mmOppName.text = opp.name;
            if (_mmOppElo != null) _mmOppElo.text = $"Elo {opp.elo}";
            if (_mmStatus != null) _mmStatus.text = "Match found!";
            yield return new WaitForSeconds(Mathf.Max(0.1f, _foundTime));

            if (_matchmakingPanel != null) _matchmakingPanel.SetActive(false);
            if (_menuRoot != null) _menuRoot.SetActive(false);
            if (_battleView != null) _battleView.BeginBattle();
        }

        void OnPull()
        {
            string id = MetaGame.Pull(out bool dup);
            RefreshSummonCoins();
            if (id == null)
            {
                if (_summonStatus != null) _summonStatus.text = "Not enough coins";
                if (_summonImage != null) _summonImage.gameObject.SetActive(false);
                if (_summonTypeBg != null) _summonTypeBg.enabled = false;
                if (_summonName != null) _summonName.text = "";
                return;
            }
            int tier = MetaGame.Rarity(id);
            var color = RarityText[Mathf.Clamp(tier, 0, 3)];
            if (_summonImage != null)
            {
                var s = MonSpriteLoader.Load(id, back: false);
                _summonImage.sprite = s;
                _summonImage.gameObject.SetActive(true);
            }
            UI.TypeBgSprites.Apply(_summonTypeBg, _dex != null && _dex.Species.TryGetValue(id, out var ssp) ? ssp : null);
            if (_summonName != null) { _summonName.text = Name(id); _summonName.color = color; }
            if (_summonStatus != null)
            {
                _summonStatus.text = dup ? $"{MetaGame.RarityNames[tier]} · duplicate (refunded)" : $"{MetaGame.RarityNames[tier]} · NEW!";
                _summonStatus.color = color;
            }
            StopAllCoroutines();
            StartCoroutine(RevealAnim(color));
        }

        // A quick reveal: rarity-colored flash + the sprite pops in with a little overshoot.
        System.Collections.IEnumerator RevealAnim(Color color)
        {
            var rt = _summonImage != null ? _summonImage.rectTransform : null;
            float t = 0f, dur = 0.45f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                // ease-out-back overshoot
                float s = 1f + 2.7f * Mathf.Pow(1f - k, 2f) * Mathf.Sin(k * Mathf.PI);
                float scale = Mathf.Lerp(0.2f, 1f, k) * (0.7f + 0.3f * s);
                if (rt != null) rt.localScale = Vector3.one * scale;
                if (_summonImage != null)
                {
                    var c = Color.Lerp(color, Color.white, k); // flash from rarity color to normal
                    _summonImage.color = c;
                }
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
            if (_summonImage != null) _summonImage.color = Color.white;
        }

        void RefreshSummonCoins()
        {
            if (_summonCoins != null) _summonCoins.text = $"{MetaGame.Profile.coins} coins";
        }

        // ---- box / team builder -------------------------------------------------------------
        void PopulateBox()
        {
            if (_boxContent == null || _monCellPrefab == null) return;
            foreach (var c in _cells) if (c != null) Destroy(c.gameObject);
            _cells.Clear();

            var team = new HashSet<string>(MetaGame.Profile.team);
            foreach (var id in MetaGame.Profile.owned)
            {
                var cell = Instantiate(_monCellPrefab, _boxContent);
                cell.gameObject.SetActive(true); // template is kept inactive
                cell.Show(id, Name(id), inTeam: team.Contains(id),
                          rarityColor: RarityText[Mathf.Clamp(MetaGame.Rarity(id), 0, 3)]);
                cell.SetSelected(id == _detailId);
                cell.Clicked += OnCellClicked;
                _cells.Add(cell);
            }
            RefreshTeamCount();
        }

        void OnCellClicked(string id)
        {
            // Tap the already-selected mon again → deselect (close the preview).
            if (id == _detailId && _detailPanel != null && _detailPanel.activeSelf)
            {
                _detailId = null;
                _detailPanel.SetActive(false);
                foreach (var c in _cells) if (c != null) c.SetSelected(false);
                return;
            }
            ShowDetail(id);
            foreach (var c in _cells) if (c != null) c.SetSelected(c.Id == id);
        }

        // ---- mon detail / move equip ----------------------------------------------------------
        string _detailId;
        readonly List<MoveCell> _moveCells = new();

        static readonly Color EquippedBg = new Color(0.16f, 0.34f, 0.20f, 0.95f);
        static readonly Color UnlockedBg = new Color(0.16f, 0.17f, 0.22f, 0.95f);
        static readonly Color LockedBg = new Color(0.10f, 0.10f, 0.13f, 0.9f);

        void ShowDetail(string id)
        {
            _detailId = id;
            // The detail UI is a docked preview pane INSIDE the Box screen — the grid stays live.
            if (_detailPanel != null) _detailPanel.SetActive(true);
            if (_detailTitle != null)
            {
                int lv = MetaGame.CurrentLevel(id), cap = MetaGame.LevelCap(id);
                _detailTitle.text = lv >= cap ? $"{Name(id)} <size=60%>Lv {lv} (MAX)</size>"
                                              : $"{Name(id)} <size=60%>Lv {lv}/{cap}</size>";
            }
            if (_detailImage != null)
            {
                var s = MonSpriteLoader.Load(id, back: false);
                _detailImage.sprite = s;
                _detailImage.gameObject.SetActive(s != null);
            }
            UI.TypeBgSprites.Apply(_detailTypeBg, _dex != null && _dex.Species.TryGetValue(id, out var dsp) ? dsp : null);
            RefreshDetail();
        }

        void RefreshDetail()
        {
            string id = _detailId;
            if (string.IsNullOrEmpty(id)) return;

            bool onTeam = MetaGame.Profile.team.Contains(id);
            if (_detailTeamLabel != null) _detailTeamLabel.text = onTeam ? "Remove from Team" : "Add to Team";

            var mm = MetaGame.GetMonMoves(id);
            if (_detailEquipCount != null) _detailEquipCount.text = $"Equipped {mm.equipped.Count}/4 — tap to swap";

            foreach (var c in _moveCells) if (c != null) Destroy(c.gameObject);
            _moveCells.Clear();
            if (_detailMoveContent == null || _moveCellPrefab == null) return;

            // Equipped first, then unlocked, then locked (with progress), strongest last within groups.
            var dex = MetaGame.Dex;
            foreach (var mv in MetaGame.MovePool(id)
                     .OrderBy(m => mm.equipped.Contains(m) ? 0 : mm.unlocked.Contains(m) ? 1 : 2)
                     .ThenBy(m => dex != null && dex.Moves.TryGetValue(m, out var d) ? d.BasePower : 0))
            {
                bool equipped = mm.equipped.Contains(mv);
                bool unlocked = mm.unlocked.Contains(mv);
                string sub, desc = "";
                Sim.MoveCategory? cat = null;
                Sim.MonType? mtype = null;
                if (dex != null && dex.Moves.TryGetValue(mv, out var md))
                {
                    sub = md.BasePower > 0 ? $"{md.Type} · {md.BasePower} BP" : $"{md.Type} · Status";
                    desc = md.ShortDesc ?? "";
                    cat = md.Category;
                    mtype = md.Type;
                }
                else sub = "";
                float progress = -1f;
                if (!unlocked)
                {
                    int i = mm.progressIds.IndexOf(mv);
                    int pts = i >= 0 ? mm.progressPts[i] : 0;
                    sub += $"   {pts}/{MetaGame.MoveUnlockCost}";
                    progress = (float)pts / MetaGame.MoveUnlockCost;
                }

                var cell = Instantiate(_moveCellPrefab, _detailMoveContent);
                cell.gameObject.SetActive(true);
                cell.Show(mv, MetaGame.MoveName(mv), sub, desc,
                          equipped ? EquippedBg : unlocked ? UnlockedBg : LockedBg,
                          unlocked ? Color.white : new Color(0.55f, 0.57f, 0.65f),
                          tappable: unlocked, progress: progress, category: cat, moveType: mtype);
                cell.Clicked += OnMoveCellClicked;
                _moveCells.Add(cell);
            }
        }

        void OnMoveCellClicked(string moveId)
        {
            if (MetaGame.ToggleEquip(_detailId, moveId)) RefreshDetail();
        }

        void OnDetailTeamToggle()
        {
            var team = new List<string>(MetaGame.Profile.team);
            if (team.Contains(_detailId)) team.Remove(_detailId);
            else if (team.Count < MetaGame.TeamSize) team.Add(_detailId);
            else return; // team full
            MetaGame.SetTeam(team);
            RefreshDetail();
            // The grid is visible behind the preview — sync the team BADGES + count live
            // (the yellow frame stays on the selected cell; it means selection, not team).
            var set = new HashSet<string>(MetaGame.Profile.team);
            foreach (var c in _cells) if (c != null) c.SetInTeam(set.Contains(c.Id));
            RefreshTeamCount();
        }

        void RefreshTeamCount()
        {
            if (_boxTeamCount != null) _boxTeamCount.text = $"Team {MetaGame.Profile.team.Count}/{MetaGame.TeamSize}";
        }
    }
}
