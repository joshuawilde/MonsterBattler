using FishNet;
using FishNet.Transporting;
using UnityEngine;

namespace MonsterBattler.Game.Net
{
    /// <summary>
    /// Connection entry points. Dedicated servers (batchmode builds or -mpserver) start listening
    /// on startup; clients call <see cref="JoinOnline"/> from the menu, which connects, submits
    /// the local team, and hands the battle to BattleView when the server starts the match.
    /// Host/port resolve from -mphost/-mpport args (Rivet lobby wiring comes later); the
    /// inspector defaults serve localhost testing.
    /// </summary>
    public sealed class NetBootstrap : MonoBehaviour
    {
        [SerializeField] string _host = "127.0.0.1";
        [SerializeField] ushort _port = 7777;
        [SerializeField] BattleView _battleView;
        [Tooltip("Editor/dev: also start a local server on join (host mode) so Battle Online works without a dedicated server — the manager's bot fills the other slot.")]
        [SerializeField] bool _hostInEditor = true;
        [Tooltip("Backend base URL (profiles/leaderboard/friends/push). Empty = backend disabled.")]
        [SerializeField] string _backendUrl = "";

        public static NetBootstrap Instance { get; private set; }

        /// <summary>Connecting / waiting for opponent — UI can poll this.</summary>
        public string Status { get; private set; } = "";

        bool _joining;

        void Awake()
        {
            Instance = this;
            string argHost = Arg("-mphost");
            if (!string.IsNullOrEmpty(argHost)) _host = argHost;
            if (ushort.TryParse(Arg("-mpport"), out var p)) _port = p;

            bool serverMode = Application.isBatchMode || HasFlag("-mpserver");
            if (serverMode)
            {
                Application.targetFrameRate = 30;
                InstanceFinder.ServerManager.StartConnection(_port);
                Debug.Log($"[Net] dedicated server listening on {_port}");
                return;
            }

            string urlArg = Arg("-backend");
            if (!string.IsNullOrEmpty(urlArg)) _backendUrl = urlArg;
            if (!string.IsNullOrEmpty(_backendUrl))
            {
                BackendApi.BaseUrl = _backendUrl;
                StartCoroutine(BackendApi.SyncProfile(Meta.MetaGame.Profile.username,
                    p => { if (p != null) Debug.Log($"[Backend] profile synced: {p}"); }));
            }
        }

        /// <summary>Menu hook: connect to the server and queue for a match with the meta team.</summary>
        public void JoinOnline()
        {
            if (_joining) return;
            _joining = true;
            Status = "Connecting…";

            if (_hostInEditor && Application.isEditor && !InstanceFinder.ServerManager.Started)
                InstanceFinder.ServerManager.StartConnection(_port);

            var cm = InstanceFinder.ClientManager;
            cm.OnClientConnectionState += OnClientState;
            cm.StartConnection(_host, _port);
        }

        void OnClientState(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
            {
                Status = "Finding opponent…";
                var mgr = NetBattleManager.Instance;
                mgr.MatchStarted += OnMatchStarted;
                // ServerRpcs only work after the scene NetworkObject initializes on this client.
                if (mgr.IsClientReady) mgr.SubmitTeam(NetTeamSpec.FromMeta());
                else
                {
                    System.Action submit = null;
                    submit = () => { mgr.ClientReady -= submit; mgr.SubmitTeam(NetTeamSpec.FromMeta()); };
                    mgr.ClientReady += submit;
                }
            }
            else if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                Status = _joining ? "Connection failed" : "";
                _joining = false;
            }
        }

        void OnMatchStarted(int mySide, ulong seed, NetTeamSpec spec0, NetTeamSpec spec1)
        {
            NetBattleManager.Instance.MatchStarted -= OnMatchStarted;
            Status = "";
            var session = new NetBattleSession(NetBattleManager.Instance, mySide);
            var view = _battleView != null ? _battleView : FindAnyObjectByType<BattleView>();
            view.BeginNetBattle(session, seed, spec0, spec1);
            OnlineMatchBegan?.Invoke();
        }

        /// <summary>Menu listens to hide itself when the online battle actually starts.</summary>
        public event System.Action OnlineMatchBegan;

        static string Arg(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }

        static bool HasFlag(string name)
        {
            foreach (var a in System.Environment.GetCommandLineArgs())
                if (a == name) return true;
            return false;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
