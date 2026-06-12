using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using MonsterBattler.Sim;
using UnityEngine;

namespace MonsterBattler.Game.Net
{
    /// <summary>
    /// Server-authoritative match orchestrator (scene NetworkObject). Clients submit their team
    /// spec, the server pairs the first two, assigns canonical sim sides by join order, picks the
    /// battle seed, and runs the authoritative sim. Each turn both sides submit inputs
    /// (replacement picks after faints, then choices); the server applies them to its sim and
    /// echoes the pair to both clients, whose deterministic mirrors stay in lockstep.
    /// If only one player is connected after <see cref="_botFillSeconds"/>, the server fills the
    /// match with an AI it drives itself — same wire protocol, so the full path tests solo.
    /// </summary>
    public sealed class NetBattleManager : NetworkBehaviour
    {
        [Tooltip("Fill with a server-side bot if a second player hasn't joined after this many seconds. 0 = wait forever.")]
        [SerializeField] float _botFillSeconds = 10f;
        [SerializeField] int _botElo = 1100;

        // ---- client-side surface (one match per scene for now) --------------------------------

        public static NetBattleManager Instance { get; private set; }

        /// <summary>Raised on the client when the server starts the match.</summary>
        public event System.Action<int /*mySide*/, ulong /*seed*/, NetTeamSpec /*side0*/, NetTeamSpec /*side1*/> MatchStarted;
        public event System.Action<int /*s0Pick*/, int /*s1Pick*/> ReplacementsResolved;
        public event System.Action<Choice /*s0*/, Choice /*s1*/> TurnResolved;
        public event System.Action MatchAborted;

        void Awake() => Instance = this;

        /// <summary>RPCs are usable only once this scene object initializes on the client.</summary>
        public bool IsClientReady { get; private set; }
        public event System.Action ClientReady;

        public override void OnStartClient()
        {
            base.OnStartClient();
            IsClientReady = true;
            ClientReady?.Invoke();
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            IsClientReady = false;
        }

        /// <summary>Client → server: join the match queue with our team.</summary>
        public void SubmitTeam(NetTeamSpec spec) => ServerSubmitTeam(spec);

        public void SubmitReplacement(int teamIndex) => ServerSubmitReplacement(teamIndex);

        public void SubmitChoice(Choice choice) => ServerSubmitChoice(choice);

        // ---- server state ----------------------------------------------------------------------

        sealed class Player
        {
            public NetworkConnection Conn;   // null for the server bot
            public NetTeamSpec Spec;
            public int? ReplPick;
            public Choice? Choice;
        }

        readonly List<Player> _players = new();
        Battle _sim;
        AI.IBattleAI _botAI;
        int _botSide = -1;
        float _aloneSince = -1f;
        bool _running;

        [ServerRpc(RequireOwnership = false)]
        void ServerSubmitTeam(NetTeamSpec spec, NetworkConnection conn = null)
        {
            if (_running || _players.Count >= 2 || spec.Species == null || spec.Species.Length == 0) return;
            foreach (var p in _players) if (p.Conn == conn) return; // duplicate join
            _players.Add(new Player { Conn = conn, Spec = spec });
            Debug.Log($"[NetBattle] team from {spec.Username} ({_players.Count}/2)");
            if (_players.Count == 2) StartMatch();
            else _aloneSince = Time.time;
        }

        void Update()
        {
            if (!IsServerStarted || _running) return;
            if (_players.Count == 1 && _botFillSeconds > 0f && Time.time - _aloneSince > _botFillSeconds)
                FillWithBot();
        }

        void FillWithBot()
        {
            var dex = DexLoader.LoadFromStreamingAssets();
            var randbats = RandbatsLoader.LoadFromStreamingAssets();
            ulong seed = (ulong)(long)System.Environment.TickCount;
            var team = new RandomTeamGenerator(dex, randbats, new Prng(seed)).GenerateTeam(
                System.Math.Max(1, _players[0].Spec.Species.Length));
            var spec = new NetTeamSpec
            {
                Username = "ServerBot", Elo = _botElo,
                Species = team.ConvertAll(m => m.Species.Id).ToArray(),
                Levels = team.ConvertAll(m => m.Level).ToArray(),
                MovesCsv = team.ConvertAll(m => string.Join(",", m.Moves.ConvertAll(s => s.Move.Id))).ToArray(),
            };
            _players.Add(new Player { Conn = null, Spec = spec });
            _botSide = 1;
            Debug.Log("[NetBattle] filling with server bot");
            StartMatch();
        }

        void StartMatch()
        {
            _running = true;
            ulong seed = (ulong)System.Guid.NewGuid().GetHashCode() * 2654435761UL + (ulong)(long)System.Environment.TickCount;

            var dex = DexLoader.LoadFromStreamingAssets();
            var randbats = RandbatsLoader.LoadFromStreamingAssets();
            _sim = new Battle(dex, seed) { ManualSwitches = true };
            var s0 = new Side { Name = _players[0].Spec.Username };
            s0.Team.AddRange(_players[0].Spec.Build(dex, randbats));
            s0.ActiveSlots.Add(s0.Team[0]); s0.Team[0].IsActive = true;
            var s1 = new Side { Name = _players[1].Spec.Username };
            s1.Team.AddRange(_players[1].Spec.Build(dex, randbats));
            s1.ActiveSlots.Add(s1.Team[0]); s1.Team[0].IsActive = true;
            _sim.Setup(s0, s1);
            _sim.Log.Lines.Clear();

            if (_botSide >= 0)
                _botAI = AI.BattleAIFactory.ForElo(_botElo, seed ^ 0x9E3779B97F4A7C15UL);

            for (int i = 0; i < 2; i++)
                if (_players[i].Conn != null)
                    TargetStartMatch(_players[i].Conn, i, seed, _players[0].Spec, _players[1].Spec);
            Debug.Log($"[NetBattle] match started: {_players[0].Spec.Username} vs {_players[1].Spec.Username} seed={seed}");
            SubmitBotInputsIfNeeded();
        }

        [TargetRpc]
        void TargetStartMatch(NetworkConnection conn, int side, ulong seed, NetTeamSpec spec0, NetTeamSpec spec1)
            => MatchStarted?.Invoke(side, seed, spec0, spec1);

        [ServerRpc(RequireOwnership = false)]
        void ServerSubmitReplacement(int teamIndex, NetworkConnection conn = null)
        {
            var p = PlayerOf(conn);
            if (p == null || !_running) return;
            p.ReplPick = teamIndex;
            TryResolveReplacements();
        }

        [ServerRpc(RequireOwnership = false)]
        void ServerSubmitChoice(Choice choice, NetworkConnection conn = null)
        {
            var p = PlayerOf(conn);
            if (p == null || !_running) return;
            p.Choice = choice;
            TryResolveTurn();
        }

        Player PlayerOf(NetworkConnection conn)
        {
            foreach (var p in _players) if (p.Conn == conn) return p;
            return null;
        }

        void TryResolveReplacements()
        {
            if (_players.Count < 2 || _players[0].ReplPick == null || _players[1].ReplPick == null) return;
            int p0 = _players[0].ReplPick.Value, p1 = _players[1].ReplPick.Value;
            _players[0].ReplPick = null; _players[1].ReplPick = null;
            if (p0 >= 0) _sim.Switch(_sim.Sides[0], p0);
            if (p1 >= 0) _sim.Switch(_sim.Sides[1], p1);
            _sim.Log.Lines.Clear();
            ObserversReplacements(p0, p1);
            SubmitBotInputsIfNeeded();
        }

        void TryResolveTurn()
        {
            if (_players.Count < 2 || _players[0].Choice == null || _players[1].Choice == null) return;
            var c0 = _players[0].Choice.Value; var c1 = _players[1].Choice.Value;
            _players[0].Choice = null; _players[1].Choice = null;
            _sim.Step(c0, c1);
            _sim.Log.Lines.Clear();
            ObserversTurn(c0, c1);
            if (_sim.IsFinished)
            {
                Debug.Log($"[NetBattle] finished, winner side {_sim.WinningSide}");
                Invoke(nameof(ResetMatch), 5f); // durable server: ready for the next pair
            }
            else SubmitBotInputsIfNeeded();
        }

        /// <summary>Server bot acts through the same submission path as a remote player.</summary>
        void SubmitBotInputsIfNeeded()
        {
            if (_botSide < 0 || _sim == null || _sim.IsFinished) return;
            var bot = _players[_botSide];
            var mySide = _sim.Sides[_botSide];
            bool needsRepl = mySide.ActiveSlots[0].IsFainted && HasAliveBench(mySide);
            bool otherNeeds = OtherNeedsReplacement();
            if (needsRepl || otherNeeds)
            {
                if (bot.ReplPick != null) return;
                int pick = -1;
                if (needsRepl)
                    for (int i = 0; i < mySide.Team.Count; i++)
                        if (mySide.Team[i] != mySide.ActiveSlots[0] && !mySide.Team[i].IsFainted) { pick = i; break; }
                bot.ReplPick = pick;
                TryResolveReplacements();
                return;
            }
            if (bot.Choice == null)
            {
                bot.Choice = _botAI.ChooseAction(_sim, mySide, _sim.Sides[1 - _botSide]);
                TryResolveTurn();
            }
        }

        bool OtherNeedsReplacement()
        {
            var other = _sim.Sides[1 - _botSide];
            return other.ActiveSlots[0].IsFainted && HasAliveBench(other);
        }

        static bool HasAliveBench(Side side)
        {
            for (int i = 0; i < side.Team.Count; i++)
                if (side.Team[i] != side.ActiveSlots[0] && !side.Team[i].IsFainted) return true;
            return false;
        }

        [ObserversRpc]
        void ObserversReplacements(int s0Pick, int s1Pick) => ReplacementsResolved?.Invoke(s0Pick, s1Pick);

        [ObserversRpc]
        void ObserversTurn(Choice c0, Choice c1) => TurnResolved?.Invoke(c0, c1);

        [ObserversRpc]
        void ObserversAbort() => MatchAborted?.Invoke();

        public override void OnDespawnServer(NetworkConnection connection)
        {
            base.OnDespawnServer(connection);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            ServerManager.OnRemoteConnectionState += OnRemoteConnState;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            if (ServerManager != null) ServerManager.OnRemoteConnectionState -= OnRemoteConnState;
        }

        void OnRemoteConnState(NetworkConnection conn, FishNet.Transporting.RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState != FishNet.Transporting.RemoteConnectionState.Stopped) return;
            if (PlayerOf(conn) == null) return;
            if (_running)
            {
                Debug.Log("[NetBattle] player disconnected — aborting match");
                ObserversAbort();
            }
            ResetMatch();
        }

        /// <summary>Clear all match state so a durable server can host the next pair.</summary>
        void ResetMatch()
        {
            _players.Clear();
            _sim = null;
            _botAI = null;
            _botSide = -1;
            _running = false;
        }
    }
}
