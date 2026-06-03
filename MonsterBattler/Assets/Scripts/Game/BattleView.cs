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

        [Header("Opponent mon info")]
        [SerializeField] Text _name1;
        [SerializeField] Text _hp1Text;
        [SerializeField] Image _hp1Fill;

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

        [Header("Demo")]
        [SerializeField] ulong _seed = 12345;
        [Tooltip("PS RandomPlayerAI 'move' bias. 1.0 = always attack, lower values cause occasional voluntary switches.")]
        [Range(0f, 1f)]
        [SerializeField] float _opponentMoveBias = 1.0f;

        Battle _battle;
        MoveButton[] _moves;
        SwitchButton[] _switches;
        Choice? _pendingChoice;
        IBattleAI _opponentAI;

        void Start()
        {
            Application.runInBackground = true;

            var dex = DexLoader.LoadFromStreamingAssets();
            _battle = new Battle(dex, _seed);

            var playerTeam = BuildTeam(dex, new (string species, string ability, string[] moves, string item)[]
            {
                ("bulbasaur",  "overgrow", new[] { "leechseed", "razorleaf", "vinewhip", "swordsdance" },     null),
                ("charmander", "blaze",    new[] { "flamethrower", "slash", "dragondance", "quickattack" },   "lifeorb"),
                ("squirtle",   "torrent",  new[] { "protect", "hydropump", "icebeam", "calmmind" },           null),
                ("pikachu",    "static",   new[] { "thunderwave", "thunderbolt", "nastyplot", "quickattack" },null),
                ("gengar",     "levitate", new[] { "willowisp", "shadowball", "thunderbolt", "nastyplot" },   null),
                ("snorlax",    "thickfat", new[] { "bulkup", "earthquake", "hypervoice", "icebeam" },         "leftovers"),
            });
            var opponentTeam = BuildTeam(dex, new (string species, string ability, string[] moves, string item)[]
            {
                ("gengar",     "levitate", new[] { "shadowball", "hypervoice", "thunderbolt", "icebeam" },    null),
                ("snorlax",    "thickfat", new[] { "stoneedge", "earthquake", "hypervoice", "icebeam" },      "leftovers"),
                ("charmander", "blaze",    new[] { "flamethrower", "slash", "ember", "quickattack" },         "lifeorb"),
                ("squirtle",   "torrent",  new[] { "hydropump", "watergun", "icebeam", "tackle" },            null),
                ("pikachu",    "static",   new[] { "thunderwave", "thunderbolt", "quickattack", "ironhead" }, null),
                ("bulbasaur",  "overgrow", new[] { "leechseed", "razorleaf", "vinewhip", "tackle" },          null),
            });

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

        void OnMoveClicked(int idx)
        {
            var player = _battle.Sides[0].ActiveSlots[0];
            if (idx >= player.Moves.Count) return;
            _pendingChoice = Choice.UseMove(player.Moves[idx].Move.Id);
        }

        void OnSwitchClicked(int idx)
        {
            var side = _battle.Sides[0];
            if (idx < 0 || idx >= side.Team.Count) return;
            var candidate = side.Team[idx];
            if (candidate.IsFainted || candidate.IsActive) return;
            _pendingChoice = Choice.SwitchTo(idx);
        }

        void SetInputEnabled(bool on)
        {
            var player = _battle.Sides[0].ActiveSlots[0];
            for (int i = 0; i < _moves.Length; i++)
            {
                if (_moves[i] == null) continue;
                bool active = i < player.Moves.Count;
                _moves[i].gameObject.SetActive(active);
                _moves[i].SetInteractable(on && active);
            }
            // Switch buttons keep their own interactable state per Show(), but a global
            // disable while a turn is animating is also useful.
            for (int i = 0; i < _switches.Length; i++)
                if (_switches[i] != null)
                {
                    var b = _switches[i].GetComponent<Button>();
                    // Show() already set interactable; we just additionally suppress while resolving.
                    if (b != null && !on) b.interactable = false;
                }
        }

        void RefreshAll()
        {
            var p0 = _battle.Sides[0].ActiveSlots[0];
            var p1 = _battle.Sides[1].ActiveSlots[0];

            if (_turnText != null) _turnText.text = $"Turn {_battle.TurnNumber + 1}";

            SetMonInfo(_name0, _hp0Text, _hp0Fill, p0);
            SetMonInfo(_name1, _hp1Text, _hp1Fill, p1);

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
        }

        static void SetMonInfo(Text name, Text hp, Image fill, Pokemon mon)
        {
            if (mon == null) return;
            int cur = mon.CurrentHp;
            int max = mon.MaxStats[(int)Stat.HP];
            int pct = max == 0 ? 0 : 100 * cur / max;
            if (name != null) name.text = $"{mon.Species?.Name ?? mon.Nickname} L{mon.Level}";
            if (hp != null)   hp.text   = $"{cur}/{max}  ({pct}%)";
            if (fill != null) fill.fillAmount = max == 0 ? 0 : (float)cur / max;
        }

        void FlushLog()
        {
            foreach (var line in _battle.Log.Lines) Debug.Log(line);
            _battle.Log.Lines.Clear();
        }
    }
}
