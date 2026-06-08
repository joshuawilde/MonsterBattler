using System.Collections.Generic;
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
        [SerializeField] Button _battleButton;
        [SerializeField] Button _boxButton;
        [SerializeField] Button _summonButton;

        [Header("Box (team builder)")]
        [SerializeField] Transform _boxContent;        // grid parent; cells spawned at runtime
        [SerializeField] MonCell _monCellPrefab;
        [SerializeField] TextMeshProUGUI _boxTeamCount;
        [SerializeField] Button _boxBackButton;

        [Header("Summon")]
        [SerializeField] Button _pullButton;
        [SerializeField] Image _summonImage;
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
            try { _dex = DexLoader.LoadFromStreamingAssets(); } catch { _dex = null; }
            Wire(_battleButton, OnBattle);
            Wire(_boxButton, ShowBox);
            Wire(_summonButton, ShowSummon);
            Wire(_boxBackButton, ShowHome);
            Wire(_summonBackButton, ShowHome);
            Wire(_pullButton, OnPull);
        }

        static void Wire(Button b, UnityEngine.Events.UnityAction a) { if (b != null) b.onClick.AddListener(a); }

        /// <summary>Called by BattleView.Start when a menu is present: show the home screen.</summary>
        public void Boot(BattleView battleView)
        {
            _battleView = battleView;
            if (_menuRoot != null) _menuRoot.SetActive(true);
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
        }

        void ShowBox()
        {
            SetPanels(home: false, box: true, summon: false);
            PopulateBox();
        }

        void ShowSummon()
        {
            SetPanels(home: false, box: false, summon: true);
            if (_summonStatus != null) _summonStatus.text = $"Summon — {MetaGame.PullCost} coins";
            if (_summonName != null) _summonName.text = "";
            if (_summonImage != null) _summonImage.gameObject.SetActive(false);
            RefreshSummonCoins();
        }

        void SetPanels(bool home, bool box, bool summon)
        {
            if (_homePanel != null) _homePanel.SetActive(home);
            if (_boxPanel != null) _boxPanel.SetActive(box);
            if (_summonPanel != null) _summonPanel.SetActive(summon);
        }

        // ---- actions ------------------------------------------------------------------------
        void OnBattle()
        {
            if (_battleView == null) _battleView = FindObjectOfType<BattleView>();
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
                cell.Show(id, Name(id), team.Contains(id), CardTint(MetaGame.Rarity(id)));
                cell.Clicked += OnCellClicked;
                _cells.Add(cell);
            }
            RefreshTeamCount();
        }

        void OnCellClicked(string id)
        {
            var team = new List<string>(MetaGame.Profile.team);
            if (team.Contains(id)) team.Remove(id);
            else if (team.Count < MetaGame.TeamSize) team.Add(id);
            else return; // team full
            MetaGame.SetTeam(team);

            var set = new HashSet<string>(MetaGame.Profile.team);
            foreach (var c in _cells) c.SetSelected(set.Contains(c.Id));
            RefreshTeamCount();
        }

        void RefreshTeamCount()
        {
            if (_boxTeamCount != null) _boxTeamCount.text = $"Team {MetaGame.Profile.team.Count}/{MetaGame.TeamSize}";
        }
    }
}
