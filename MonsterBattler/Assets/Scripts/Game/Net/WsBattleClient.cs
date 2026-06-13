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
    /// The socket read loop runs off-thread and only enqueues messages; <see cref="Poll"/> drains
    /// them on the main thread (call it every frame), so all session state the coroutines read is
    /// touched on the main thread — no locks. BattleView is unchanged.
    /// </summary>
    public sealed class WsBattleClient : IBattleSession
    {
        public int MySide { get; private set; }
        public bool Aborted { get; private set; }

        /// <summary>Fires (main thread) when the server starts the match — wire to BeginNetBattle.</summary>
        public event Action<int /*side*/, ulong /*seed*/, NetTeamSpec, NetTeamSpec> MatchStarted;
        public event Action<string /*from*/, string /*text*/> ChatReceived;

        readonly ClientWebSocket _ws = new();
        readonly ConcurrentQueue<string> _inbox = new();
        readonly string _url, _matchId, _uid;
        readonly NetTeamSpec _team;

        int? _theirRepl;
        Choice? _theirChoice;
        bool _started;

        public WsBattleClient(string wsUrl, string matchId, string uid, NetTeamSpec team)
        {
            _url = wsUrl; _matchId = matchId; _uid = uid; _team = team;
        }

        public async Task ConnectAsync()
        {
            await _ws.ConnectAsync(new Uri(_url), CancellationToken.None);
            Send(new JObject { ["t"] = "join", ["matchId"] = _matchId, ["uid"] = _uid, ["team"] = TeamJson(_team) });
            _ = ReadLoop();
        }

        async Task ReadLoop()
        {
            var buf = new byte[8192];
            var sb = new StringBuilder();
            while (_ws.State == WebSocketState.Open)
            {
                WebSocketReceiveResult r;
                try { r = await _ws.ReceiveAsync(buf, CancellationToken.None); }
                catch { break; }
                if (r.MessageType == WebSocketMessageType.Close) break;
                sb.Append(Encoding.UTF8.GetString(buf, 0, r.Count));
                if (!r.EndOfMessage) continue;
                _inbox.Enqueue(sb.ToString());
                sb.Clear();
            }
            _inbox.Enqueue("{\"t\":\"abort\"}"); // socket closed → treat as abort if mid-match
        }

        /// <summary>Drain inbound messages on the main thread. Call every frame while online.</summary>
        public void Poll()
        {
            while (_inbox.TryDequeue(out var json))
            {
                JObject m;
                try { m = JObject.Parse(json); } catch { continue; }
                switch ((string)m["t"])
                {
                    case "start":
                        MySide = (int)m["side"];
                        _started = true;
                        MatchStarted?.Invoke(MySide, (ulong)m["seed"], ReadTeam(m["team0"]), ReadTeam(m["team1"]));
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

        public void SendChat(string text)
            => Send(new JObject { ["t"] = "chat", ["text"] = text });

        // ---- json helpers (explicit camelCase to match the server) -----------------------------

        void Send(JObject o)
        {
            if (_ws.State != WebSocketState.Open) return;
            var bytes = Encoding.UTF8.GetBytes(o.ToString(Newtonsoft.Json.Formatting.None));
            _ = _ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public void Close()
        {
            try { _ = _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None); } catch { }
        }

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
