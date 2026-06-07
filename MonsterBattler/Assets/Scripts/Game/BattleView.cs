using System.Collections;
using System.Collections.Generic;
using MonsterBattler.Game.AI;
using MonsterBattler.Game.UI;
using MonsterBattler.Sim;
using MonsterBattler.Sim.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

namespace MonsterBattler.Game
{
    /// <summary>
    /// Drives a battle and reflects its state onto the scene. Per project rule, every visual
    /// element (sphere slots, HP bars, name/HP labels, move buttons, switch buttons, turn pill)
    /// is authored in the scene and assigned through these serialized fields.
    /// </summary>
    public sealed class BattleView : MonoBehaviour
    {
        [Header("Scene-authored slot transforms")]
        [SerializeField] Transform _slot0;
        [SerializeField] Transform _slot1;

        [Header("Player mon info")]
        [SerializeField] TextMeshProUGUI _name0;
        [SerializeField] TextMeshProUGUI _hp0Text;
        [SerializeField] Image _hp0Fill;
        [SerializeField] TextMeshProUGUI _status0;   // status badge (BRN/PAR/…), optional

        [Header("Opponent mon info")]
        [SerializeField] TextMeshProUGUI _name1;
        [SerializeField] TextMeshProUGUI _hp1Text;
        [SerializeField] Image _hp1Fill;
        [SerializeField] TextMeshProUGUI _status1;   // status badge, optional

        [Header("Field / side conditions (scene-authored Text, optional)")]
        [SerializeField] TextMeshProUGUI _fieldText;   // weather / terrain / Trick Room
        [SerializeField] TextMeshProUGUI _sideText0;   // player hazards / screens / tailwind
        [SerializeField] TextMeshProUGUI _sideText1;   // opponent hazards / screens / tailwind

        [Header("Active stat-boost chips (scene-authored HorizontalLayoutGroup rows + chip prefab)")]
        [SerializeField] Transform _boostRow0;         // player: parent (HLG) for boost chips
        [SerializeField] Transform _boostRow1;         // opponent: parent (HLG) for boost chips
        [SerializeField] UI.TypeBadge _statChipPrefab; // chip prefab instantiated per boost

        [Header("Turn counter")]
        [SerializeField] TextMeshProUGUI _turnText;

        [Header("Move buttons (scene-authored)")]
        [SerializeField] MoveButton _move0;
        [SerializeField] MoveButton _move1;
        [SerializeField] MoveButton _move2;
        [SerializeField] MoveButton _move3;

        [Header("Switch row (6 buttons, scene-authored)")]
        [SerializeField] SwitchButton _switch0;
        [SerializeField] SwitchButton _switch1;
        [SerializeField] SwitchButton _switch2;
        [SerializeField] SwitchButton _switch3;
        [SerializeField] SwitchButton _switch4;
        [SerializeField] SwitchButton _switch5;

        [Header("Terastallize (scene-authored)")]
        [SerializeField] Button _teraButton;
        [SerializeField] TextMeshProUGUI _teraLabel;

        [Header("Opponent roster (scene-authored: parent of 6 RosterIcon chips)")]
        [SerializeField] Transform _opponentRosterParent;

        [Header("Battle log feed (scene-authored Text)")]
        [SerializeField] TextMeshProUGUI _logText;       // persistent debug feed (usually hidden)
        [SerializeField] UI.MessageBar _messageBar;      // Showdown-style ephemeral message bar

        [Header("Info overlay (scene-authored)")]
        [SerializeField] UI.InfoPanel _infoPanel;

        [Header("Floating combat text (scene-authored anchors + prefab)")]
        [SerializeField] Transform _popupAnchor0;        // over the player's mon
        [SerializeField] Transform _popupAnchor1;        // over the opponent's mon
        [SerializeField] UI.FloatingText _floatingTextPrefab;

        [Header("End screen (scene-authored)")]
        [SerializeField] GameObject _endScreen;          // full-screen overlay shown when the battle ends
        [SerializeField] TextMeshProUGUI _endResultText; // "Victory!" / "Defeat!" / "Draw"
        [SerializeField] Button _rematchButton;          // reloads the scene for a new battle

        [Header("Demo")]
        [SerializeField] ulong _seed = 12345;
        [Tooltip("PS RandomPlayerAI 'move' bias. 1.0 = always attack, lower values cause occasional voluntary switches.")]
        [Range(0f, 1f)]
        [SerializeField] float _opponentMoveBias = 1.0f;
        [Tooltip("Generate Showdown-style gen9 random-battle teams instead of the hardcoded demo teams.")]
        [SerializeField] bool _useRandomTeams = true;
        [Tooltip("Seconds between each beat of a turn (move, damage, faint, …) during playback.")]
        [Range(0f, 2f)]
        [SerializeField] float _turnStepDelay = 0.7f;

        Battle _battle;
        MoveButton[] _moves;
        SwitchButton[] _switches;
        UI.RosterIcon[] _oppRoster = System.Array.Empty<UI.RosterIcon>();
        readonly List<string> _logFeed = new();
        const int MaxLogLines = 14;
        Choice? _pendingChoice;
        IBattleAI _opponentAI;
        bool _isInForcedSwitch;
        int _pendingForcedSwitchIdx = -1;
        bool _teraQueued;

        // HP-bar animation: lerp the displayed fill toward the true value each frame, snapping on
        // switch-in (a new mon shouldn't drain/refill from the previous mon's HP).
        const float HpLerpPerSecond = 1.6f;
        Image[] _hpFills;
        TextMeshProUGUI[] _hpTexts;
        TextMeshProUGUI[] _nameTexts;
        readonly float[] _hpShown = { 1f, 1f };
        readonly float[] _hpTarget = { 1f, 1f };
        readonly Pokemon[] _hpLastMon = new Pokemon[2];

        void Start()
        {
            Application.runInBackground = true;

            var dex = DexLoader.LoadFromStreamingAssets();
            _battle = new Battle(dex, _seed);

            List<Pokemon> playerTeam, opponentTeam;
            if (_useRandomTeams)
            {
                var randbats = RandbatsLoader.LoadFromStreamingAssets();
                // Fork independent PRNGs off the seed so each side's team is reproducible.
                playerTeam   = new RandomTeamGenerator(dex, randbats, new Prng(_seed)).GenerateTeam();
                opponentTeam = new RandomTeamGenerator(dex, randbats, new Prng(_seed ^ 0x9E3779B97F4A7C15UL)).GenerateTeam();
            }
            else
            {
                playerTeam = BuildTeam(dex, new (string species, string ability, string[] moves, string item)[]
                {
                    ("bulbasaur",  "overgrow", new[] { "leechseed", "razorleaf", "vinewhip", "swordsdance" },     null),
                    ("charmander", "blaze",    new[] { "flamethrower", "slash", "dragondance", "quickattack" },   "lifeorb"),
                    ("squirtle",   "torrent",  new[] { "protect", "hydropump", "icebeam", "calmmind" },           null),
                    ("pikachu",    "static",   new[] { "thunderwave", "thunderbolt", "nastyplot", "quickattack" },null),
                    ("gengar",     "levitate", new[] { "willowisp", "shadowball", "thunderbolt", "nastyplot" },   null),
                    ("snorlax",    "thickfat", new[] { "bulkup", "earthquake", "hypervoice", "icebeam" },         "leftovers"),
                });
                opponentTeam = BuildTeam(dex, new (string species, string ability, string[] moves, string item)[]
                {
                    ("gengar",     "levitate", new[] { "shadowball", "hypervoice", "thunderbolt", "icebeam" },    null),
                    ("snorlax",    "thickfat", new[] { "stoneedge", "earthquake", "hypervoice", "icebeam" },      "leftovers"),
                    ("charmander", "blaze",    new[] { "flamethrower", "slash", "ember", "quickattack" },         "lifeorb"),
                    ("squirtle",   "torrent",  new[] { "hydropump", "watergun", "icebeam", "tackle" },            null),
                    ("pikachu",    "static",   new[] { "thunderwave", "thunderbolt", "quickattack", "ironhead" }, null),
                    ("bulbasaur",  "overgrow", new[] { "leechseed", "razorleaf", "vinewhip", "tackle" },          null),
                });
            }

            var side0 = new Side { Name = "Player" };
            side0.Team.AddRange(playerTeam);
            side0.ActiveSlots.Add(playerTeam[0]); playerTeam[0].IsActive = true;
            var side1 = new Side { Name = "Opponent" };
            side1.Team.AddRange(opponentTeam);
            side1.ActiveSlots.Add(opponentTeam[0]); opponentTeam[0].IsActive = true;
            _battle.Setup(side0, side1);

            _opponentAI = new RandomPlayerAI(_opponentMoveBias);

            _moves = new[] { _move0, _move1, _move2, _move3 };
            _switches = new[] { _switch0, _switch1, _switch2, _switch3, _switch4, _switch5 };
            _hpFills = new[] { _hp0Fill, _hp1Fill };
            _hpTexts = new[] { _hp0Text, _hp1Text };
            _nameTexts = new[] { _name0, _name1 };
            _oppRoster = _opponentRosterParent != null
                ? _opponentRosterParent.GetComponentsInChildren<UI.RosterIcon>(includeInactive: true)
                : System.Array.Empty<UI.RosterIcon>();
            for (int i = 0; i < _oppRoster.Length; i++)
            {
                int idx = i;
                if (_oppRoster[i] != null) _oppRoster[i].Clicked += () => OnOppRosterClicked(idx);
            }

            for (int i = 0; i < _moves.Length; i++)
            {
                int idx = i;
                if (_moves[i] != null) _moves[i].Clicked += () => OnMoveClicked(idx);
            }
            for (int i = 0; i < _switches.Length; i++)
            {
                int idx = i;
                if (_switches[i] != null) _switches[i].Clicked += () => OnSwitchClicked(idx);
            }
            if (_teraButton != null) _teraButton.onClick.AddListener(OnTeraClicked);
            if (_infoPanel != null)
            {
                _infoPanel.CloseRequested += OnCloseInfo;
                _infoPanel.SwapRequested += OnSwapRequested;
                _infoPanel.SetVisible(false); // hidden until requested
            }
            if (_rematchButton != null) _rematchButton.onClick.AddListener(OnRematch);
            if (_endScreen != null) _endScreen.SetActive(false); // hidden until the battle ends

            _logFeed.Add("Battle started!");
            FlushLog(); // surface any lead switch-in / weather / ability activations from Setup
            RefreshAll();
            StartCoroutine(BattleLoop());
        }

        List<Pokemon> BuildTeam(Dex dex, (string species, string ability, string[] moves, string item)[] entries)
        {
            var team = new List<Pokemon>();
            foreach (var (species, ability, moves, item) in entries)
                team.Add(DemoDex.MakeBattler(dex, species, species, ability, moves, item));
            return team;
        }

        IEnumerator BattleLoop()
        {
            while (!_battle.IsFinished)
            {
                // Forced switch: if our active fainted (engine no longer auto-switches us),
                // require the player to pick a replacement before the next turn.
                if (_battle.Sides[0].ActiveSlots[0].IsFainted && HasAliveBench(_battle.Sides[0]))
                {
                    yield return PromptForcedSwitch();
                    if (_battle.IsFinished) break;
                }

                _pendingChoice = null;
                SetInputEnabled(true);
                while (_pendingChoice == null) yield return null;
                SetInputEnabled(false);

                var playerChoice = _pendingChoice.Value;
                var opponentChoice = _opponentAI.ChooseAction(_battle, _battle.Sides[1], _battle.Sides[0]);

                // The sim resolves the whole turn at once and records an ordered log; capture the
                // names that were active going in, then replay that log beat-by-beat with delays.
                var preActive = new[] { MonName(_battle.Sides[0].ActiveSlots[0]), MonName(_battle.Sides[1].ActiveSlots[0]) };
                _battle.Step(playerChoice, opponentChoice);
                var lines = new List<string>(_battle.Log.Lines);
                _battle.Log.Lines.Clear();

                yield return PlaybackTurn(lines, preActive);
                RefreshAll(); // final authoritative sync (status badges, field, stat stages, roster)
                yield return new WaitForSeconds(0.3f);
            }
            SetInputEnabled(false);
            ShowEndScreen();
        }

        void ShowEndScreen()
        {
            if (_endResultText != null)
            {
                (string label, Color color) = _battle.WinningSide switch
                {
                    0    => ("Victory!", new Color(0.30f, 0.78f, 0.33f)),
                    1    => ("Defeat",   new Color(0.86f, 0.25f, 0.22f)),
                    _    => ("Draw",     new Color(0.80f, 0.80f, 0.85f)),
                };
                _endResultText.text = label;
                _endResultText.color = color;
            }
            if (_endScreen != null) _endScreen.SetActive(true);
        }

        void OnRematch() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        IEnumerator PromptForcedSwitch()
        {
            // Disable moves; only the (non-fainted) switch buttons should be tappable.
            _isInForcedSwitch = true;
            _pendingForcedSwitchIdx = -1;
            SetForcedSwitchUI(true);
            while (_pendingForcedSwitchIdx < 0) yield return null;
            _battle.Switch(_battle.Sides[0], _pendingForcedSwitchIdx);
            _isInForcedSwitch = false;
            FlushLog();
            RefreshAll();
            yield return new WaitForSeconds(0.4f);
        }

        static bool HasAliveBench(Side side)
        {
            for (int i = 0; i < side.Team.Count; i++)
                if (side.Team[i] != side.ActiveSlots[0] && !side.Team[i].IsFainted) return true;
            return false;
        }

        void SetForcedSwitchUI(bool on)
        {
            foreach (var b in _moves) if (b != null) b.SetInteractable(false);
            // Switch buttons obey their own per-mon Show() interactability — RefreshAll already
            // ran with the post-faint state, so fainted/active mons are correctly grayed out.
        }

        void OnMoveClicked(int idx)
        {
            if (_isInForcedSwitch) return; // moves locked during forced-switch prompt
            var player = _battle.Sides[0].ActiveSlots[0];
            if (idx >= player.Moves.Count) return;
            _pendingChoice = Choice.UseMove(player.Moves[idx].Move.Id, _teraQueued);
            _teraQueued = false;
        }

        Pokemon _inspectTarget;
        bool _acceptingInput;

        void OnOppRosterClicked(int idx)
        {
            var team = _battle.Sides[1].Team;
            if (idx >= 0 && idx < team.Count) Inspect(team[idx]);
        }

        /// <summary>Open the info panel on a specific mon (any side, active or benched). Shows a
        /// Swap button when it's one of your benched mons and a switch is currently allowed.</summary>
        void Inspect(Pokemon mon)
        {
            if (_infoPanel == null || mon == null) return;
            _inspectTarget = mon;
            _infoPanel.SetVisible(true);
            _infoPanel.Show(mon, CanSwap(mon));
        }

        /// <summary>True if <paramref name="mon"/> is a legal switch target for the player right now.</summary>
        bool CanSwap(Pokemon mon)
        {
            var side = _battle.Sides[0];
            if (!side.Team.Contains(mon)) return false;          // must be your mon
            if (mon.IsFainted || mon == side.ActiveSlots[0]) return false; // not fainted / not already out
            return _isInForcedSwitch || _acceptingInput;          // a state that accepts a switch
        }

        void OnCloseInfo()
        {
            if (_infoPanel != null) _infoPanel.SetVisible(false);
        }

        void OnSwapRequested()
        {
            if (_inspectTarget == null || !CanSwap(_inspectTarget)) return;
            int idx = _battle.Sides[0].Team.IndexOf(_inspectTarget);
            if (idx < 0) return;
            if (_isInForcedSwitch) _pendingForcedSwitchIdx = idx;
            else _pendingChoice = Choice.SwitchTo(idx);
            if (_infoPanel != null) _infoPanel.SetVisible(false);
        }

        void OnTeraClicked()
        {
            var side = _battle.Sides[0];
            var player = side.ActiveSlots[0];
            if (side.HasUsedTera || player.IsTerastallized || player.TeraType == MonType.None) return;
            _teraQueued = !_teraQueued;
            RefreshTeraLabel();
        }

        void RefreshTeraLabel()
        {
            if (_teraLabel == null) return;
            var side = _battle.Sides[0];
            var player = side.ActiveSlots[0];
            if (player.IsTerastallized)      _teraLabel.text = $"Tera: {player.TeraType} (active)";
            else if (side.HasUsedTera)       _teraLabel.text = "Tera (used)";
            else                             _teraLabel.text = _teraQueued
                ? $"Tera: {player.TeraType} [armed]"
                : $"Tera: {player.TeraType}";
            if (_teraButton != null)
                _teraButton.interactable = !side.HasUsedTera && !player.IsTerastallized && player.TeraType != MonType.None;
        }

        // A "switch button" is now just a portrait button: tapping any of your mons inspects it.
        // The actual swap happens from the info panel's Swap button (see OnSwapRequested).
        void OnSwitchClicked(int idx)
        {
            var side = _battle.Sides[0];
            if (idx >= 0 && idx < side.Team.Count) Inspect(side.Team[idx]);
        }

        void SetInputEnabled(bool on)
        {
            _acceptingInput = on; // gates whether the panel offers a Swap action
            var player = _battle.Sides[0].ActiveSlots[0];
            // Choice items lock the player into one move. Disable everything else when locked.
            bool isLocked = !string.IsNullOrEmpty(player.LockedMoveId);
            for (int i = 0; i < _moves.Length; i++)
            {
                if (_moves[i] == null) continue;
                bool active = i < player.Moves.Count;
                _moves[i].gameObject.SetActive(active);
                bool isLockedMove = isLocked && active && player.Moves[i].Move.Id == player.LockedMoveId;
                _moves[i].SetInteractable(on && active && (!isLocked || isLockedMove));
            }
            // Portrait/switch buttons stay tappable at all times so you can inspect any mon; the
            // open panel re-evaluates whether a swap is currently legal.
            if (_infoPanel != null && _infoPanel.IsVisible && _inspectTarget != null)
                _infoPanel.Show(_inspectTarget, CanSwap(_inspectTarget));
        }

        void Update()
        {
            if (_hpFills == null) return;
            for (int i = 0; i < _hpFills.Length; i++)
            {
                if (_hpFills[i] == null) continue;
                _hpShown[i] = Mathf.MoveTowards(_hpShown[i], _hpTarget[i], HpLerpPerSecond * Time.deltaTime);
                _hpFills[i].fillAmount = _hpShown[i];
                _hpFills[i].color = HpColor(_hpShown[i]);
            }
        }

        void SetHpTarget(int side, Pokemon mon)
        {
            int max = mon.MaxStats[(int)Stat.HP];
            _hpTarget[side] = max == 0 ? 0f : (float)mon.CurrentHp / max;
            if (mon != _hpLastMon[side]) { _hpShown[side] = _hpTarget[side]; _hpLastMon[side] = mon; } // snap on switch
        }

        static Color HpColor(float frac) =>
            frac > 0.5f ? new Color(0.30f, 0.78f, 0.33f) :
            frac > 0.2f ? new Color(0.95f, 0.77f, 0.20f) :
                          new Color(0.86f, 0.25f, 0.22f);

        void RefreshAll()
        {
            var p0 = _battle.Sides[0].ActiveSlots[0];
            var p1 = _battle.Sides[1].ActiveSlots[0];

            if (_turnText != null) _turnText.text = $"Turn {_battle.TurnNumber + 1}";

            SetMonInfo(_name0, _hp0Text, p0);
            SetMonInfo(_name1, _hp1Text, p1);
            SetHpTarget(0, p0);
            SetHpTarget(1, p1);
            SetStatusBadge(_status0, p0);
            SetStatusBadge(_status1, p1);

            if (_fieldText != null) _fieldText.text = FieldStatusText.Field(_battle);
            if (_sideText0 != null) _sideText0.text = FieldStatusText.Side(_battle.Sides[0]);
            if (_sideText1 != null) _sideText1.text = FieldStatusText.Side(_battle.Sides[1]);
            PopulateBoostChips(_boostRow0, p0);
            PopulateBoostChips(_boostRow1, p1);

            for (int i = 0; i < _moves.Length; i++)
            {
                if (_moves[i] == null) continue;
                _moves[i].Show(i < p0.Moves.Count ? p0.Moves[i] : null);
            }
            var team = _battle.Sides[0].Team;
            for (int i = 0; i < _switches.Length; i++)
            {
                if (_switches[i] == null) continue;
                _switches[i].Show(i < team.Count ? team[i] : null, isActive: i < team.Count && team[i] == p0);
            }

            var oppTeam = _battle.Sides[1].Team;
            for (int i = 0; i < _oppRoster.Length; i++)
            {
                if (_oppRoster[i] == null) continue;
                _oppRoster[i].Show(i < oppTeam.Count ? oppTeam[i] : null,
                                   isActive: i < oppTeam.Count && oppTeam[i] == p1);
            }
            // Keep the open panel current (the inspected mon's HP/status may have changed).
            if (_infoPanel != null && _infoPanel.IsVisible)
            {
                var target = _inspectTarget ?? p0;
                _infoPanel.Show(target, CanSwap(target));
            }
            RefreshTeraLabel();
        }

        static void SetMonInfo(TextMeshProUGUI name, TextMeshProUGUI hp, Pokemon mon)
        {
            if (mon == null) return;
            int cur = mon.CurrentHp;
            int max = mon.MaxStats[(int)Stat.HP];
            int pct = max == 0 ? 0 : 100 * cur / max;
            if (name != null) name.text = $"{mon.Species?.Name ?? mon.Nickname} L{mon.Level}";
            if (hp != null)   hp.text   = $"{cur}/{max}  ({pct}%)";
        }

        static readonly (Stat stat, string label)[] BoostStats =
        {
            (Stat.Atk, "Atk"), (Stat.Def, "Def"), (Stat.SpA, "SpA"), (Stat.SpD, "SpD"), (Stat.Spe, "Spe"),
        };

        // Rebuild a nameplate's boost row: one chip prefab per active stat stage (cyan up / red down).
        void PopulateBoostChips(Transform row, Pokemon mon)
        {
            if (row == null) return;
            for (int i = row.childCount - 1; i >= 0; i--) Destroy(row.GetChild(i).gameObject);
            if (mon == null || _statChipPrefab == null) return;
            foreach (var (stat, label) in BoostStats)
            {
                int stage = mon.StatStages[(int)stat];
                if (stage == 0) continue;
                var chip = Instantiate(_statChipPrefab, row);
                var color = stage > 0 ? new Color(0.30f, 0.78f, 1f) : new Color(1f, 0.42f, 0.42f);
                chip.SetChip($"{label} ×{Stats.StageMult(stage):0.##}", color);
            }
        }

        static void SetStatusBadge(TextMeshProUGUI badge, Pokemon mon)
        {
            if (badge == null) return;
            if (mon == null || mon.Status == StatusCondition.None) { badge.text = ""; return; }
            (string label, Color color) = mon.Status switch
            {
                StatusCondition.Burn          => ("BRN", new Color(0.94f, 0.42f, 0.20f)),
                StatusCondition.Paralysis     => ("PAR", new Color(0.95f, 0.80f, 0.20f)),
                StatusCondition.Poison        => ("PSN", new Color(0.64f, 0.34f, 0.74f)),
                StatusCondition.BadlyPoisoned => ("TOX", new Color(0.50f, 0.18f, 0.55f)),
                StatusCondition.Sleep         => ("SLP", new Color(0.60f, 0.60f, 0.65f)),
                StatusCondition.Freeze        => ("FRZ", new Color(0.40f, 0.75f, 0.90f)),
                StatusCondition.Frostbite     => ("FRB", new Color(0.55f, 0.70f, 0.95f)),
                _ => ("", Color.white),
            };
            badge.text = label;
            badge.color = color;
        }

        void FlushLog()
        {
            foreach (var line in _battle.Log.Lines)
            {
                var readable = BattleLogFormatter.Format(line);
                if (!string.IsNullOrEmpty(readable)) AppendLog(readable);
            }
            _battle.Log.Lines.Clear();
        }

        void AppendLog(string line)
        {
            // Persistent debug feed only (usually hidden). The Showdown-style message box is driven
            // per-action by PlaybackTurn (BeginGroup/AppendLine/FadeOut).
            _logFeed.Add(line);
            while (_logFeed.Count > MaxLogLines) _logFeed.RemoveAt(0);
            if (_logText != null) _logText.text = string.Join("\n", _logFeed);
        }

        // Replays one turn's protocol log entry-by-entry: reveals each readable line into the feed,
        // animates the HP bars to the value embedded in each damage/heal line, and re-labels a slot
        // when a mon switches in — pausing _turnStepDelay between beats so the turn plays in sequence.
        static readonly Color HealBg = new Color(0.27f, 0.62f, 0.30f);
        static readonly Color DmgBg = new Color(0.78f, 0.27f, 0.24f);
        static readonly Color BoostBg = new Color(0.24f, 0.50f, 0.85f);
        static readonly Color DropBg = new Color(0.80f, 0.50f, 0.18f);
        static readonly Color StatusBg = new Color(0.55f, 0.30f, 0.62f);
        static readonly Color MissBg = new Color(0.42f, 0.42f, 0.48f);
        static readonly Color AbilityBg = new Color(0.28f, 0.45f, 0.72f);
        static readonly Color FaintBg = new Color(0.32f, 0.20f, 0.22f);
        static readonly Color SwitchBg = new Color(0.26f, 0.52f, 0.46f);

        void SpawnPopup(int side, string text, Color bg)
        {
            var anchor = side == 0 ? _popupAnchor0 : _popupAnchor1;
            if (anchor == null || _floatingTextPrefab == null) return;
            var p = Instantiate(_floatingTextPrefab, anchor);
            ((RectTransform)p.transform).anchoredPosition = Vector2.zero;
            p.Show(text, bg);
        }

        static string StatAbbr(string s) => s switch
        {
            "atk" => "Atk", "def" => "Def", "spa" => "SpA", "spd" => "SpD",
            "spe" => "Spe", "accuracy" => "Acc", "evasion" => "Eva", _ => s,
        };

        IEnumerator PlaybackTurn(List<string> lines, string[] active)
        {
            bool groupActive = false; // a message box is currently showing an action
            foreach (var raw in lines)
            {
                var parts = raw.Split('|'); // ["", tag, arg1, arg2, ...]
                string tag = parts.Length > 1 ? parts[1] : "";
                bool beat = false;

                var readable = BattleLogFormatter.Format(raw);
                if (!string.IsNullOrEmpty(readable)) { AppendLog(readable); beat = true; }

                // Message box: group lines by action. A "major" line (move/switch/faint/cant — no
                // leading '-') starts a fresh box; minor '-' effect lines stack under it (box grows).
                if (_messageBar != null)
                {
                    if (tag == "turn")
                    {
                        if (groupActive) { _messageBar.FadeOut(); groupActive = false; }
                    }
                    else if (!string.IsNullOrEmpty(readable))
                    {
                        if (!tag.StartsWith("-"))
                        {
                            if (groupActive) { _messageBar.FadeOut(); yield return new WaitForSeconds(0.28f); }
                            _messageBar.BeginGroup(readable);
                            groupActive = true;
                        }
                        else if (groupActive) _messageBar.AppendLine(readable);
                        else { _messageBar.BeginGroup(readable); groupActive = true; }
                    }
                }

                if ((tag == "-damage" || tag == "-heal" || tag == "-sethp") && parts.Length > 3)
                {
                    int side = SideForName(parts[2], active);
                    if (side >= 0)
                    {
                        float oldFrac = _hpTarget[side];
                        ApplyHpFromLog(side, parts[3], snap: false);
                        int d = Mathf.RoundToInt((_hpTarget[side] - oldFrac) * 100f);
                        if (tag != "-sethp" && d != 0)
                            SpawnPopup(side, (d > 0 ? "+" : "") + d + "%", d > 0 ? HealBg : DmgBg);
                        beat = true;
                    }
                }
                else if (tag == "switch" && parts.Length > 3)
                {
                    int side = SideOfTeamName(parts[2]);
                    if (side >= 0)
                    {
                        active[side] = parts[2];
                        var mon = FindByName(parts[2]);
                        if (mon != null && _nameTexts != null && _nameTexts[side] != null)
                            _nameTexts[side].text = $"{MonName(mon)} L{mon.Level}";
                        ApplyHpFromLog(side, parts[3], snap: true); // new mon — no drain animation
                        SpawnPopup(side, $"Go! {MonName(mon) ?? parts[2]}", SwitchBg);
                        beat = true;
                    }
                }
                else if (tag == "faint" && parts.Length > 2)
                {
                    int side = SideForName(parts[2], active);
                    if (side >= 0) { SpawnPopup(side, "Fainted", FaintBg); beat = true; }
                }
                else if ((tag == "-boost" || tag == "-unboost") && parts.Length > 4)
                {
                    int side = SideForName(parts[2], active);
                    bool up = tag == "-boost";
                    if (side >= 0) { SpawnPopup(side, $"{StatAbbr(parts[3])} {(up ? "+" : "−")}{parts[4]}", up ? BoostBg : DropBg); beat = true; }
                }
                else if (tag == "-status" && parts.Length > 3)
                {
                    int side = SideForName(parts[2], active);
                    if (side >= 0) { SpawnPopup(side, parts[3].ToUpperInvariant(), StatusBg); beat = true; }
                }
                else if (tag == "-crit" && parts.Length > 2)
                {
                    int side = SideForName(parts[2], active);
                    if (side >= 0) { SpawnPopup(side, "Crit!", DmgBg); beat = true; }
                }
                else if (tag == "-miss" && parts.Length > 3)
                {
                    int side = SideForName(parts[3], active);
                    if (side >= 0) { SpawnPopup(side, "Miss", MissBg); beat = true; }
                }
                else if (tag == "-fail" && parts.Length > 2)
                {
                    int side = SideForName(parts[2], active);
                    if (side >= 0) { SpawnPopup(side, "Failed", MissBg); beat = true; }
                }
                else if ((tag == "-activate" || tag == "-ability") && parts.Length > 3)
                {
                    int side = SideForName(parts[2], active);
                    string name = parts[3].StartsWith("ability: ") ? parts[3].Substring(9) : parts[3];
                    if (side >= 0 && tag == "-ability") { SpawnPopup(side, name, AbilityBg); beat = true; }
                    else if (side >= 0 && parts[3].StartsWith("ability: ")) { SpawnPopup(side, name, AbilityBg); beat = true; }
                }

                if (beat && _turnStepDelay > 0f) yield return new WaitForSeconds(_turnStepDelay);
            }
            if (_messageBar != null && groupActive) _messageBar.FadeOut(); // hide after the turn
        }

        // Parse "cur/max" and drive the side's HP bar + text. snap=true jumps instantly (switch-in).
        void ApplyHpFromLog(int side, string hp, bool snap)
        {
            int slash = hp.IndexOf('/');
            if (slash < 0) return;
            if (!int.TryParse(hp.Substring(0, slash), out int cur)) return;
            if (!int.TryParse(hp.Substring(slash + 1), out int max) || max <= 0) return;
            float frac = Mathf.Clamp01((float)cur / max);
            _hpTarget[side] = frac;
            if (snap) _hpShown[side] = frac;
            if (_hpTexts != null && _hpTexts[side] != null)
                _hpTexts[side].text = $"{cur}/{max}  ({100 * cur / max}%)";
        }

        static string MonName(Pokemon m) => m?.Nickname ?? m?.Species?.Name;

        int SideForName(string name, string[] active)
        {
            if (name == active[0]) return 0;
            if (name == active[1]) return 1;
            return SideOfTeamName(name);
        }

        int SideOfTeamName(string name)
        {
            for (int s = 0; s < 2; s++)
                foreach (var m in _battle.Sides[s].Team)
                    if (MonName(m) == name) return s;
            return -1;
        }

        Pokemon FindByName(string name)
        {
            foreach (var s in _battle.Sides)
                foreach (var m in s.Team)
                    if (MonName(m) == name) return m;
            return null;
        }
    }
}
