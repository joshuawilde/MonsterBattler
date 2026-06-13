using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using MonsterBattler.Sim;
using UnityEngine;

namespace MonsterBattler.Game.Net
{
    /// <summary>
    /// WebSocket transport to the pure-dotnet battle server, implementing <see cref="IBattleSession"/>.
    /// Uses the built-in <see cref="ClientWebSocket"/> over plain ws:// — Unity's Mono TLS can't
    /// complete a wss handshake (hangs in the editor and on iOS), so the relay is unencrypted; only
    /// move/switch choices and chat cross it. The receive loop runs off-thread and only enqueues;
    /// <see cref="Poll"/> drains on the main thread (call every frame) so battle coroutines read
    /// session state lock-free. BattleView is unchanged.
    /// </summary>
    public sealed class WsBattleClient : IBattleSession
    {
        public int MySide { get; private set; }
        public bool Aborted { get; private set; }
        public bool ForfeitWin { get; private set; }

        public event Action<int /*side*/, ulong /*seed*/, NetTeamSpec, NetTeamSpec> MatchStarted;
        public event Action<string /*from*/, string /*text*/> ChatReceived;

        readonly ClientWebSocket _ws = new();
        readonly ConcurrentQueue<string> _inbox = new();
        readonly CancellationTokenSource _cts = new();
        readonly string _wsUrl, _matchId, _uid;
        readonly NetTeamSpec _team;

        int? _theirRepl;
        Choice? _theirChoice;
        bool _started;

        public WsBattleClient(string wsUrl, string matchId, string uid, NetTeamSpec team)
        {
            _wsUrl = wsUrl; _matchId = matchId; _uid = uid; _team = team;
        }

        public async Task ConnectAsync()
        {
            Debug.Log($"[Ws] ConnectAsync: dialing {_wsUrl}");
            await _ws.ConnectAsync(new Uri(_wsUrl), _cts.Token);
            Debug.Log($"[Ws] connected (state={_ws.State}); sending join");
            // AWAIT the join so it's actually transmitted before we consider ourselves connected —
            // a fire-and-forget here can race the socket-open and get dropped (the server then never
            // starts the match). Start the receive loop only after the join is on the wire.
            await SendNow(new JObject { ["t"] = "join", ["matchId"] = _matchId, ["uid"] = _uid, ["team"] = TeamJson(_team) });
            _ = ReceiveLoop();
        }

        async Task ReceiveLoop()
        {
            Debug.Log("[Ws] receive loop started");
            var buf = new byte[8192];
            var sb = new StringBuilder();
            try
            {
                while (_ws.State == WebSocketState.Open && !_cts.IsCancellationRequested)
                {
                    var res = await _ws.ReceiveAsync(new ArraySegment<byte>(buf), _cts.Token);
                    if (res.MessageType == WebSocketMessageType.Close) { Debug.Log($"[Ws] recv Close frame: {res.CloseStatus} {res.CloseStatusDescription}"); break; }
                    sb.Append(Encoding.UTF8.GetString(buf, 0, res.Count));
                    if (res.EndOfMessage) { var s = sb.ToString(); Debug.Log($"[Ws] recv {s.Substring(0, Math.Min(90, s.Length))}"); _inbox.Enqueue(s); sb.Clear(); }
                }
                Debug.Log($"[Ws] receive loop exiting normally (state={_ws.State}, cancelled={_cts.IsCancellationRequested})");
            }
            catch (Exception e) when (!(e is OperationCanceledException))
            {
                Debug.LogWarning($"[Ws] receive loop EXCEPTION: {e.GetBaseException().Message}");
            }
            _inbox.Enqueue("{\"t\":\"abort\"}");
        }

        public void Poll()
        {
            while (_inbox.TryDequeue(out var json))
            {
                JObject m;
                try { m = JObject.Parse(json); } catch (Exception e) { Debug.LogWarning($"[Ws] bad json: {e.Message}"); continue; }
                string mt = (string)m["t"];
                Debug.Log($"[Ws] dispatch '{mt}'");
                switch (mt)
                {
                    case "start":
                        try
                        {
                            MySide = (int)m["side"];
                            _started = true;
                            MatchStarted?.Invoke(MySide, (ulong)m["seed"], ReadTeam(m["team0"]), ReadTeam(m["team1"]));
                        }
                        catch (Exception e) { Debug.LogError($"[Ws] 'start' handling threw: {e}"); Aborted = true; }
                        break;
                    case "turn":
                        _theirChoice = ReadChoice(MySide == 0 ? m["s1"] : m["s0"]);
                        break;
                    case "replace":
                        _theirRepl = MySide == 0 ? (int)m["p1"] : (int)m["p0"];
                        break;
                    case "chat":
                        ChatReceived?.Invoke((string)m["from"], (string)m["text"]);
                        break;
                    case "forfeit":
                        ForfeitWin = true;
                        Aborted = true;
                        break;
                    case "abort":
                        if (_started) Aborted = true;
                        break;
                    case "error":
                        Debug.LogWarning($"[Ws] server error: {m["text"]}");
                        Aborted = true;
                        break;
                }
            }
        }

        public IEnumerator ExchangeReplacements(int mine, Action<int> onTheirs)
        {
            _theirRepl = null;
            Send(new JObject { ["t"] = "replace", ["index"] = mine });
            while (_theirRepl == null && !Aborted) yield return null;
            if (_theirRepl != null) onTheirs(_theirRepl.Value);
        }

        public IEnumerator ExchangeTurn(Choice mine, Action<Choice> onTheirs)
        {
            _theirChoice = null;
            Send(new JObject { ["t"] = "choice", ["choice"] = ChoiceJson(mine) });
            while (_theirChoice == null && !Aborted) yield return null;
            if (_theirChoice != null) onTheirs(_theirChoice.Value);
        }

        public void SendChat(string text) => Send(new JObject { ["t"] = "chat", ["text"] = text });

        // fire-and-forget wrapper for in-battle sends (choices/replace/chat)
        async void Send(JObject o)
        {
            try { await SendNow(o); }
            catch (Exception e) { Debug.LogWarning($"[Ws] send '{(string)o["t"]}' FAILED: {e.GetBaseException().Message}"); }
        }

        async Task SendNow(JObject o)
        {
            string t = (string)o["t"];
            if (_ws.State != WebSocketState.Open)
            {
                Debug.LogWarning($"[Ws] send '{t}' SKIPPED — socket state={_ws.State}");
                return;
            }
            var bytes = Encoding.UTF8.GetBytes(o.ToString(Newtonsoft.Json.Formatting.None));
            await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
            Debug.Log($"[Ws] sent '{t}' ({bytes.Length}b)");
        }

        public void Close()
        {
            try { _cts.Cancel(); } catch { }
            try { _ = _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None); } catch { }
        }

        // ---- json helpers (explicit camelCase to match the server) -----------------------------

        static JObject TeamJson(NetTeamSpec t) => new()
        {
            ["uid"] = t.Uid, ["username"] = t.Username, ["elo"] = t.Elo,
            ["species"] = new JArray(t.Species), ["levels"] = new JArray(t.Levels),
            ["movesCsv"] = new JArray(t.MovesCsv),
        };

        static NetTeamSpec ReadTeam(JToken j) => new()
        {
            Uid = (string)j["uid"], Username = (string)j["username"], Elo = (int)j["elo"],
            Species = j["species"].ToObject<string[]>(),
            Levels = j["levels"].ToObject<int[]>(),
            MovesCsv = j["movesCsv"].ToObject<string[]>(),
        };

        static JObject ChoiceJson(Choice c) => new()
        {
            ["kind"] = c.Kind == ChoiceKind.Switch ? "switch" : "move",
            ["moveId"] = c.MoveId, ["switchToIndex"] = c.SwitchToIndex,
            ["terastallize"] = c.Terastallize, ["pivotToIndexPlusOne"] = c.PivotToIndexPlusOne,
        };

        static Choice ReadChoice(JToken j)
        {
            if (j == null) return Choice.UseMove(null);
            if ((string)j["kind"] == "switch") return Choice.SwitchTo((int)j["switchToIndex"]);
            return new Choice
            {
                Kind = ChoiceKind.Move,
                MoveId = (string)j["moveId"],
                Terastallize = (bool)j["terastallize"],
                PivotToIndexPlusOne = (int)j["pivotToIndexPlusOne"],
            };
        }
    }
}
