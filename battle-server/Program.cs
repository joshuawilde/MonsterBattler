using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MonsterBattler.Sim;
using MonsterBattler.Sim.Data;

namespace MonsterBattler.BattleServer
{
    public static class Program
    {
        static readonly ConcurrentDictionary<string, MatchRegistration> Registered = new();
        static readonly ConcurrentDictionary<string, Match> Active = new();

        static Dex _dex;
        static RandbatsDex _randbats;
        static string _internalKey;
        static string _backendUrl;
        static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

        public static void Main(string[] args)
        {
            _dex = DexData.Dex;
            _randbats = DexData.Randbats;
            _internalKey = Environment.GetEnvironmentVariable("INTERNAL_API_KEY") ?? "";
            _backendUrl = (Environment.GetEnvironmentVariable("BACKEND_URL") ?? "").TrimEnd('/');
            var port = Environment.GetEnvironmentVariable("PORT") ?? "8081";
            Console.WriteLine($"[battle-server] dex {_dex.Species.Count} species, {_dex.Moves.Count} moves; backend={(_backendUrl == "" ? "(none)" : _backendUrl)}");

            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
            var app = builder.Build();
            app.UseWebSockets();

            app.MapGet("/healthz", () => Results.Text("ok"));

            // Backend registers a paired match before clients connect (shared-key auth).
            app.MapPost("/internal/match", async (HttpContext ctx) =>
            {
                if (!KeyOk(ctx)) return Results.StatusCode(403);
                var reg = await System.Text.Json.JsonSerializer.DeserializeAsync<MatchRegistration>(
                    ctx.Request.Body, Connection.Json);
                if (reg == null || string.IsNullOrEmpty(reg.MatchId)) return Results.BadRequest();
                Registered[reg.MatchId] = reg;
                Console.WriteLine($"[battle-server] registered match {reg.MatchId}: {reg.Uid0} vs {reg.Uid1}");
                return Results.Ok(new { ok = true });
            });

            app.Map("/ws", async (HttpContext ctx) =>
            {
                if (!ctx.WebSockets.IsWebSocketRequest) { ctx.Response.StatusCode = 400; return; }
                using var ws = await ctx.WebSockets.AcceptWebSocketAsync();
                await HandleSocket(new Connection(ws));
            });

            Console.WriteLine($"[battle-server] listening on :{port}");
            app.Run();
        }

        static bool KeyOk(HttpContext ctx)
            => _internalKey != "" && ctx.Request.Headers["X-Api-Key"] == _internalKey;

        static async Task HandleSocket(Connection conn)
        {
            // First frame must be a join.
            var first = await conn.ReceiveAsync(default);
            if (first == null || first.T != "join" || string.IsNullOrEmpty(first.MatchId) || string.IsNullOrEmpty(first.Uid))
            {
                await conn.SendAsync(new ServerMsg { T = "error", Text = "expected join" });
                await conn.CloseAsync();
                return;
            }
            if (!Registered.TryGetValue(first.MatchId, out var reg) || (first.Uid != reg.Uid0 && first.Uid != reg.Uid1))
            {
                await conn.SendAsync(new ServerMsg { T = "error", Text = "unknown match or player" });
                await conn.CloseAsync();
                return;
            }
            conn.Uid = first.Uid;
            conn.MatchId = first.MatchId;

            var match = Active.GetOrAdd(first.MatchId, id => new Match(reg, _dex, _randbats, Report, OnMatchDone));
            match.Join(conn, first.Team);

            // Pump this client's subsequent messages into the match until the socket closes.
            while (true)
            {
                var msg = await conn.ReceiveAsync(default);
                if (msg == null) break;
                switch (msg.T)
                {
                    case "choice": if (msg.Choice != null) match.OnChoice(conn.Uid, msg.Choice); break;
                    case "replace": match.OnReplace(conn.Uid, msg.Index); break;
                    case "chat": match.OnChat(conn.Uid, msg.Text); break;
                }
            }
            match.OnDisconnect(conn);
        }

        static void OnMatchDone(Match m)
        {
            Active.TryRemove(m.Id, out _);
            Registered.TryRemove(m.Id, out _);
        }

        // Report the result to the Go backend (server-side Elo). PvP only; bot matches skip this.
        static async Task Report(string uid0, string uid1, int winnerSide)
        {
            if (_backendUrl == "" || _internalKey == "") return;
            try
            {
                var body = $"{{\"uid0\":\"{uid0}\",\"uid1\":\"{uid1}\",\"winnerSide\":{winnerSide}}}";
                using var req = new HttpRequestMessage(HttpMethod.Post, _backendUrl + "/v1/internal/match-result")
                { Content = new StringContent(body, Encoding.UTF8, "application/json") };
                req.Headers.Add("X-Api-Key", _internalKey);
                var resp = await Http.SendAsync(req);
                Console.WriteLine($"[battle-server] result reported: {(int)resp.StatusCode}");
            }
            catch (Exception e) { Console.WriteLine($"[battle-server] report failed: {e.Message}"); }
        }
    }
}
