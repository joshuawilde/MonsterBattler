using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MonsterBattler.BattleServer
{
    /// <summary>One client WebSocket. WS requires serialized sends, so all sends go through a
    /// single async lock. JSON uses camelCase to match the Unity client.</summary>
    public sealed class Connection
    {
        public static readonly JsonSerializerOptions Json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        readonly WebSocket _ws;
        readonly SemaphoreSlim _sendLock = new(1, 1);

        public string Uid;       // set after join
        public string MatchId;

        public Connection(WebSocket ws) => _ws = ws;

        public bool Open => _ws.State == WebSocketState.Open;

        public async Task SendAsync(ServerMsg msg)
        {
            if (!Open) return;
            var bytes = JsonSerializer.SerializeToUtf8Bytes(msg, Json);
            await _sendLock.WaitAsync();
            try { await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None); }
            catch { /* peer gone; pump handles disconnect */ }
            finally { _sendLock.Release(); }
        }

        /// <summary>Await one text frame; null when the socket closes.</summary>
        public async Task<ClientMsg> ReceiveAsync(CancellationToken ct)
        {
            var buf = new byte[8192];
            var sb = new StringBuilder();
            while (true)
            {
                WebSocketReceiveResult r;
                try { r = await _ws.ReceiveAsync(buf, ct); }
                catch { return null; }
                if (r.MessageType == WebSocketMessageType.Close) return null;
                sb.Append(Encoding.UTF8.GetString(buf, 0, r.Count));
                if (r.EndOfMessage) break;
            }
            try { return JsonSerializer.Deserialize<ClientMsg>(sb.ToString(), Json); }
            catch { return new ClientMsg { T = "bad" }; }
        }

        public async Task CloseAsync()
        {
            try { await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None); }
            catch { }
        }
    }
}
