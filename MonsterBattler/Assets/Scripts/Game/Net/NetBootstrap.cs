using System.Collections;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MonsterBattler.Game.Net
{
    /// <summary>
    /// Online entry point. Queues through the backend matchmaker, which pairs players and returns
    /// a battle-server WebSocket address; connects there via <see cref="WsBattleClient"/> and hands
    /// the battle to BattleView when the server starts it. No Unity-server / FishNet anymore — the
    /// battle server is a pure-dotnet WebSocket service.
    /// </summary>
    public sealed class NetBootstrap : MonoBehaviour
    {
        [SerializeField] BattleView _battleView;
        [Tooltip("Backend base URL (profiles/leaderboard/matchmaking). Empty = online disabled.")]
        [SerializeField] string _backendUrl = "";

        public static NetBootstrap Instance { get; private set; }

        /// <summary>Connecting / searching / status text for the menu.</summary>
        public string Status { get; private set; } = "";
        public event System.Action OnlineMatchBegan;

        /// <summary>The active battle's session — chat UI sends through this.</summary>
        public WsBattleClient Active { get; private set; }

        bool _joining;

        void Awake()
        {
            Instance = this;
            string urlArg = Arg("-backend");
            if (!string.IsNullOrEmpty(urlArg)) _backendUrl = urlArg;
            if (!string.IsNullOrEmpty(_backendUrl))
            {
                BackendApi.BaseUrl = _backendUrl;
                BackendApi.Configured = true;
                // initial profile sync happens in FirebaseBootstrap after sign-in resolves
            }
        }

        void Update() => Active?.Poll(); // drain inbound WS messages on the main thread

        public void JoinOnline()
        {
            if (_joining) return;
            if (!BackendApi.Configured) { Status = "Online unavailable"; return; }
            _joining = true;
            StartCoroutine(MatchmakeAndConnect());
        }

        IEnumerator MatchmakeAndConnect()
        {
            Status = "Finding opponent…";
            bool queued = false;
            yield return BackendApi.MatchQueue(r => queued = r != null);
            if (!queued) { Fail("Matchmaking unavailable"); yield break; }

            float waited = 0f;
            while (waited < 90f)
            {
                JObject st = null;
                yield return BackendApi.MatchStatus(r => st = r);
                switch ((string)st?["state"])
                {
                    case "ready":
                        Status = st["opponent"] != null ? $"Match found vs {st["opponent"]}!" : "Match found!";
                        yield return Connect((string)st["wsUrl"], (string)st["matchId"]);
                        yield break;
                    case "error":
                        Fail((string)st?["error"] ?? "Matchmaking failed"); yield break;
                }
                waited += 1.5f;
                yield return new WaitForSeconds(1.5f);
            }
            StartCoroutine(BackendApi.MatchCancel());
            Fail("No match found");
        }

        IEnumerator Connect(string wsUrl, string matchId)
        {
            var client = new WsBattleClient(wsUrl, matchId, BackendApi.Uid, NetTeamSpec.FromMeta());
            client.MatchStarted += (side, seed, s0, s1) =>
            {
                Status = "";
                var view = _battleView != null ? _battleView : FindAnyObjectByType<BattleView>();
                view.BeginNetBattle(client, seed, s0, s1);
                OnlineMatchBegan?.Invoke();
            };
            Active = client;

            var connectTask = client.ConnectAsync();
            while (!connectTask.IsCompleted) yield return null;
            if (connectTask.IsFaulted)
            {
                Debug.LogWarning($"[Net] ws connect failed: {connectTask.Exception?.GetBaseException().Message}");
                Active = null;
                Fail("Couldn't reach the battle server");
            }
            // else: wait for the server's "start" (Poll → MatchStarted). _joining stays true until then.
        }

        void Fail(string reason)
        {
            Status = reason;
            _joining = false;
        }

        static string Arg(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Active?.Close();
        }
    }
}
