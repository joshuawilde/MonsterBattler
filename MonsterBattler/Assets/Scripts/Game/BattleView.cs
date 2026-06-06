using System.Collections;
using System.Collections.Generic;
using MonsterBattler.Game.AI;
using MonsterBattler.Game.UI;
using MonsterBattler.Sim;
using MonsterBattler.Sim.Data;
using UnityEngine;
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
        [SerializeField] Text _name0;
        [SerializeField] Text _hp0Text;
        [SerializeField] Image _hp0Fill;
        [SerializeField] Text _status0;   // status badge (BRN/PAR/…), optional

        [Header("Opponent mon info")]
        [SerializeField] Text _name1;
        [SerializeField] Text _hp1Text;
        [SerializeField] Image _hp1Fill;
        [SerializeField] Text _status1;   // status badge, optional

        [Header("Field / side conditions (scene-authored Text, optional)")]
        [SerializeField] Text _fieldText;   // weather / terrain / Trick Room
        [SerializeField] Text _sideText0;   // player hazards / screens / tailwind
        [SerializeField] Text _sideText1;   // opponent hazards / screens / tailwind

        [Header("Turn counter")]
        [SerializeField] Text _turnText;

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
        [SerializeField] Text _teraLabel;

        [Header("Opponent roster (scene-authored: parent of 6 RosterIcon chips)")]
        [SerializeField] Transform _opponentRosterParent;

        [Header("Battle log feed (scene-authored Text)")]
        [SerializeField] Text _logText;

        [Header("Info overlay (scene-authored)")]
        [SerializeField] UI.InfoPanel _infoPanel;

        [Header("Demo")]
        [SerializeField] ulong _seed = 12345;
        [Tooltip("PS RandomPlayerAI 'move' bias. 1.0 = always attack, lower values cause occasional voluntary switches.")]
        [Range(0f, 1f)]
        [SerializeField] float _opponentMoveBias = 1.0f;
        [Tooltip("Generate Showdown-style gen9 random-battle teams instead of the hardcoded demo teams.")]
        [SerializeField] bool _useRandomTeams = true;

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
                _battle.Step(playerChoice, opponentChoice);

                FlushLog();
                RefreshAll();
                yield return new WaitForSeconds(0.6f);
            }
            SetInputEnabled(false);
            Debug.Log($"[Battle] Winner: side {_battle.WinningSide}");
        }

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

        static void SetMonInfo(Text name, Text hp, Pokemon mon)
        {
            if (mon == null) return;
            int cur = mon.CurrentHp;
            int max = mon.MaxStats[(int)Stat.HP];
            int pct = max == 0 ? 0 : 100 * cur / max;
            if (name != null) name.text = $"{mon.Species?.Name ?? mon.Nickname} L{mon.Level}";
            if (hp != null)   hp.text   = $"{cur}/{max}  ({pct}%)";
        }

        static void SetStatusBadge(Text badge, Pokemon mon)
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
                if (!string.IsNullOrEmpty(readable)) _logFeed.Add(readable);
            }
            _battle.Log.Lines.Clear();
            while (_logFeed.Count > MaxLogLines) _logFeed.RemoveAt(0);
            if (_logText != null) _logText.text = string.Join("\n", _logFeed);
        }
    }
}
