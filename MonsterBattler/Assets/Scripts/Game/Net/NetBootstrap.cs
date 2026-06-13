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
            Debug.Log($"[Net] JoinOnline: queueing (uid={BackendApi.Uid}, backend={BackendApi.BaseUrl})");
            bool queued = false;
            yield return BackendApi.MatchQueue(r => queued = r != null);
            Debug.Log($"[Net] queue request returned: queued={queued}");
            if (!queued) { Fail("Matchmaking unavailable"); yield break; }

            float waited = 0f;
            string lastState = null;
            while (waited < 90f)
            {
                JObject st = null;
                yield return BackendApi.MatchStatus(r => st = r);
                string state = (string)st?["state"];
                if (state != lastState) { Debug.Log($"[Net] status → {state ?? "null"} (raw: {st?.ToString(Newtonsoft.Json.Formatting.None)})"); lastState = state; }
                switch (state)
                {
                    case "ready":
                        Status = st["opponent"] != null ? $"Match found vs {st["opponent"]}!" : "Match found!";
                        Debug.Log($"[Net] READY: wsUrl={st["wsUrl"]} matchId={st["matchId"]} side={st["side"]} opp={st["opponent"]}");
                        yield return Connect((string)st["wsUrl"], (string)st["matchId"]);
                        yield break;
                    case "error":
                        Debug.LogWarning($"[Net] matchmaking error: {st?["error"]}");
                        Fail((string)st?["error"] ?? "Matchmaking failed"); yield break;
                }
                waited += 1.5f;
                yield return new WaitForSeconds(1.5f);
            }
            Debug.LogWarning("[Net] matchmaking timed out after 90s — cancelling");
            StartCoroutine(BackendApi.MatchCancel());
            Fail("No match found");
        }

        IEnumerator Connect(string wsUrl, string matchId)
        {
            if (Active != null) { Debug.Log("[Net] closing stale client before reconnect"); Active.Close(); Active = null; }
            Debug.Log($"[Net] Connect: creating client for match {matchId} @ {wsUrl}");
            var client = new WsBattleClient(wsUrl, matchId, BackendApi.Uid, NetTeamSpec.FromMeta());
            client.MatchStarted += (side, seed, s0, s1) =>
            {
                Debug.Log($"[Net] MatchStarted fired: side={side} seed={seed} → BeginNetBattle");
                Status = "";
                var view = _battleView != null ? _battleView : FindAnyObjectByType<BattleView>();
                if (view == null) { Debug.LogError("[Net] no BattleView found — cannot start battle!"); return; }
                view.BeginNetBattle(client, seed, s0, s1);
                OnlineMatchBegan?.Invoke();
                Debug.Log("[Net] BeginNetBattle returned OK");
            };
            Active = client;

            Debug.Log("[Net] awaiting ConnectAsync…");
            var connectTask = client.ConnectAsync();
            float ct = 0f;
            while (!connectTask.IsCompleted) { ct += Time.unscaledDeltaTime; yield return null; }
            Debug.Log($"[Net] ConnectAsync completed in {ct:0.0}s (faulted={connectTask.IsFaulted})");
            if (connectTask.IsFaulted)
            {
                Debug.LogWarning($"[Net] ws connect failed: {connectTask.Exception?.GetBaseException()}");
                Active = null;
                Fail("Couldn't reach the battle server");
                yield break;
            }
            Debug.Log("[Net] connected — waiting for server 'start' (watch for [Ws] recv lines)");
            // _joining stays true until MatchStarted fires (Poll → MatchStarted → BeginNetBattle).
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
