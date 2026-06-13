using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MonsterBattler.Sim;
using MonsterBattler.Sim.Data;
using MonsterBattler.Game.AI;

namespace MonsterBattler.BattleServer
{
    /// <summary>
    /// One battle. A single consumer task drains an event channel, so the sim is only ever touched
    /// by one thread (no locks, no cross-match contention) while thousands of matches run in
    /// parallel. Ports NetBattleManager's server-authoritative flow: pair two teams, run the pure
    /// sim, broadcast each turn's inputs; both clients mirror it deterministically. If only one
    /// real player connects within the fill window, an AI fills the other slot.
    /// </summary>
    public sealed class Match
    {
        public string Id { get; }
        readonly Dex _dex;
        readonly RandbatsDex _randbats;
        readonly Func<string, string, int, Task> _report; // (uid0, uid1, winnerSide) → backend
        readonly Action<Match> _onDone;
        readonly Channel<Ev> _ch = Channel.CreateUnbounded<Ev>();
        readonly int _botFillMs;

        sealed class Player
        {
            public string Uid;
            public Connection Conn;       // null = bot
            public TeamSpec Spec;
            public Choice? Choice;
            public int? ReplPick;
            public bool IsBot => Conn == null;
        }

        readonly Player[] _p = { null, null }; // sim side 0 / 1
        readonly string _uid0, _uid1;
        Battle _sim;
        IBattleAI _botAI;
        int _botSide = -1;
        bool _started, _finished;
        ulong _seed;

        enum EvKind { Join, Choice, Replace, Chat, Disconnect, FillTick }
        readonly struct Ev
        {
            public EvKind Kind { get; init; }
            public string Uid { get; init; }
            public Connection Conn { get; init; }
            public TeamSpec Team { get; init; }
            public ChoiceDto Choice { get; init; }
            public int Index { get; init; }
            public string Text { get; init; }
        }

        readonly bool _botMatch; // solo match — fill the bot the instant the player joins

        public Match(MatchRegistration reg, Dex dex, RandbatsDex randbats,
                     Func<string, string, int, Task> report, Action<Match> onDone, int botFillMs = 8000)
        {
            Id = reg.MatchId; _uid0 = reg.Uid0; _uid1 = reg.Uid1; _botMatch = reg.Bot;
            _dex = dex; _randbats = randbats; _report = report; _onDone = onDone; _botFillMs = botFillMs;
            _ = Task.Run(Pump);
            _ = FillTimer();
        }

        // ---- producers (called from connection read loops; thread-safe via the channel) -------

        public void Join(Connection conn, TeamSpec team)
            => _ch.Writer.TryWrite(new Ev { Kind = EvKind.Join, Uid = conn.Uid, Conn = conn, Team = team });
        public void OnChoice(string uid, ChoiceDto c)
            => _ch.Writer.TryWrite(new Ev { Kind = EvKind.Choice, Uid = uid, Choice = c });
        public void OnReplace(string uid, int idx)
            => _ch.Writer.TryWrite(new Ev { Kind = EvKind.Replace, Uid = uid, Index = idx });
        public void OnChat(string uid, string text)
            => _ch.Writer.TryWrite(new Ev { Kind = EvKind.Chat, Uid = uid, Text = text });
        public void OnDisconnect(Connection conn)
            => _ch.Writer.TryWrite(new Ev { Kind = EvKind.Disconnect, Conn = conn });

        async Task FillTimer()
        {
            await Task.Delay(_botFillMs);
            _ch.Writer.TryWrite(new Ev { Kind = EvKind.FillTick });
        }

        int SideOf(string uid) => uid == _uid0 ? 0 : uid == _uid1 ? 1 : -1;

        // ---- single consumer ------------------------------------------------------------------

        async Task Pump()
        {
            await foreach (var ev in _ch.Reader.ReadAllAsync())
            {
                try { await Handle(ev); }
                catch (Exception e) { Console.WriteLine($"[match {Id}] error: {e.Message}"); }
                if (_finished) break;
            }
        }

        async Task Handle(Ev ev)
        {
            switch (ev.Kind)
            {
                case EvKind.Join:
                {
                    int side = SideOf(ev.Uid);
                    if (side < 0) { await ev.Conn.SendAsync(Err("not a player in this match")); return; }
                    _p[side] = new Player { Uid = ev.Uid, Conn = ev.Conn, Spec = ev.Team };
                    if (_p[0] != null && _p[1] != null) await Start();
                    else if (_botMatch) { FillBotAndStart(); await StartContinue(); } // solo: start now, no wait
                    else await ev.Conn.SendAsync(new ServerMsg { T = "waiting" });
                    break;
                }
                case EvKind.FillTick:
                    if (!_started) { FillBotAndStart(); await StartContinue(); }
                    break;
                case EvKind.Choice:
                {
                    int side = SideOf(ev.Uid);
                    if (side >= 0 && _p[side] != null) _p[side].Choice = ev.Choice.ToChoice();
                    await TryResolveTurn();
                    break;
                }
                case EvKind.Replace:
                {
                    int side = SideOf(ev.Uid);
                    if (side >= 0 && _p[side] != null) _p[side].ReplPick = ev.Index;
                    await TryResolveReplacements();
                    break;
                }
                case EvKind.Chat:
                {
                    int side = SideOf(ev.Uid);
                    var from = side >= 0 && _p[side] != null ? _p[side].Spec?.Username : "?";
                    await SendOther(side, new ServerMsg { T = "chat", From = from, Text = Trim(ev.Text) });
                    break;
                }
                case EvKind.Disconnect:
                {
                    int side = ev.Conn?.Uid != null ? SideOf(ev.Conn.Uid) : -1;
                    if (_started && !_finished && side >= 0)
                    {
                        if (_botSide < 0)
                        {
                            // Mid-match disconnect in a PvP game = forfeit: the other player wins,
                            // both get their Elo change. (No bot swap-in.)
                            int winner = 1 - side;
                            Console.WriteLine($"[match {Id}] {_p[side]?.Spec?.Username} left — forfeit, side {winner} wins");
                            if (_report != null) await _report(_uid0, _uid1, winner);
                            await SendOther(side, new ServerMsg { T = "forfeit" });
                        }
                        else
                        {
                            await SendOther(side, new ServerMsg { T = "abort" }); // vs a bot — no ranked result
                        }
                    }
                    Finish();
                    break;
                }
            }
        }

        void FillBotAndStart()
        {
            // pick the unfilled side; the other must already be a real joined player
            int empty = _p[0] == null ? 0 : _p[1] == null ? 1 : -1;
            int real = 1 - empty;
            if (empty < 0 || _p[real] == null) return; // nobody to play against → just wait/expire
            int size = Math.Max(1, _p[real].Spec?.Species?.Length ?? 6);
            _seed = NextSeed();
            var botSpec = TeamBuilder.BotSpec(_dex, _randbats, size, _seed, "Bot", _p[real].Spec?.Elo ?? 1000);
            _p[empty] = new Player { Uid = empty == 0 ? _uid0 : _uid1, Conn = null, Spec = botSpec };
            _botSide = empty;
        }

        async Task Start()
        {
            _seed = NextSeed();
            await StartContinue();
        }

        async Task StartContinue()
        {
            if (_started || _p[0] == null || _p[1] == null) return;
            _started = true;

            _sim = new Battle(_dex, _seed) { ManualSwitches = true };
            var s0 = new Side { Name = _p[0].Spec.Username };
            s0.Team.AddRange(TeamBuilder.Build(_dex, _randbats, _p[0].Spec));
            var s1 = new Side { Name = _p[1].Spec.Username };
            s1.Team.AddRange(TeamBuilder.Build(_dex, _randbats, _p[1].Spec));
            if (s0.Team.Count == 0 || s1.Team.Count == 0)
            {
                Console.WriteLine($"[match {Id}] empty team (0:{s0.Team.Count} 1:{s1.Team.Count}) — aborting");
                await Broadcast(new ServerMsg { T = "error", Text = "invalid team" });
                Finish();
                return;
            }
            s0.ActiveSlots.Add(s0.Team[0]); s0.Team[0].IsActive = true;
            s1.ActiveSlots.Add(s1.Team[0]); s1.Team[0].IsActive = true;
            _sim.Setup(s0, s1);
            _sim.Log.Lines.Clear();

            if (_botSide >= 0)
                _botAI = BattleAIFactory.ForElo(Math.Max(800, _p[_botSide].Spec.Elo), _seed ^ 0x9E3779B97F4A7C15UL);

            for (int i = 0; i < 2; i++)
                if (_p[i].Conn != null)
                    await _p[i].Conn.SendAsync(new ServerMsg
                    { T = "start", Side = i, Seed = _seed, Team0 = _p[0].Spec, Team1 = _p[1].Spec });

            Console.WriteLine($"[match {Id}] started {_p[0].Spec.Username} vs {_p[1].Spec.Username}");
            SubmitBotIfNeeded();
        }

        async Task TryResolveReplacements()
        {
            if (!_started || _finished || _p[0].ReplPick == null || _p[1].ReplPick == null) return;
            int p0 = _p[0].ReplPick.Value, p1 = _p[1].ReplPick.Value;
            _p[0].ReplPick = null; _p[1].ReplPick = null;
            if (p0 >= 0) _sim.Switch(_sim.Sides[0], p0);
            if (p1 >= 0) _sim.Switch(_sim.Sides[1], p1);
            _sim.Log.Lines.Clear();
            await Broadcast(new ServerMsg { T = "replace", P0 = p0, P1 = p1 });
            SubmitBotIfNeeded();
        }

        async Task TryResolveTurn()
        {
            if (!_started || _finished || _p[0].Choice == null || _p[1].Choice == null) return;
            var c0 = _p[0].Choice.Value; var c1 = _p[1].Choice.Value;
            _p[0].Choice = null; _p[1].Choice = null;
            _sim.Step(c0, c1);
            _sim.Log.Lines.Clear();
            await Broadcast(new ServerMsg { T = "turn", S0 = ChoiceDto.From(c0), S1 = ChoiceDto.From(c1) });
            if (_sim.IsFinished)
            {
                int winner = _sim.WinningSide ?? -1;
                Console.WriteLine($"[match {Id}] finished, winner side {winner}");
                if (_botSide < 0 && _report != null)
                    await _report(_uid0, _uid1, winner);
                Finish();
            }
            else SubmitBotIfNeeded();
        }

        // bot acts through the same path as a remote player
        void SubmitBotIfNeeded()
        {
            if (_botSide < 0 || _sim == null || _sim.IsFinished) return;
            var bot = _p[_botSide];
            var mine = _sim.Sides[_botSide];
            var theirs = _sim.Sides[1 - _botSide];
            bool iNeed = mine.ActiveSlots[0].IsFainted && HasAliveBench(mine);
            bool theyNeed = theirs.ActiveSlots[0].IsFainted && HasAliveBench(theirs);
            if (iNeed || theyNeed)
            {
                if (bot.ReplPick != null) return;
                int pick = -1;
                if (iNeed)
                    for (int i = 0; i < mine.Team.Count; i++)
                        if (mine.Team[i] != mine.ActiveSlots[0] && !mine.Team[i].IsFainted) { pick = i; break; }
                bot.ReplPick = pick;
                _ = TryResolveReplacements();
                return;
            }
            if (bot.Choice == null)
            {
                bot.Choice = _botAI.ChooseAction(_sim, mine, theirs);
                _ = TryResolveTurn();
            }
        }

        static bool HasAliveBench(Side side)
        {
            for (int i = 0; i < side.Team.Count; i++)
                if (side.Team[i] != side.ActiveSlots[0] && !side.Team[i].IsFainted) return true;
            return false;
        }

        async Task Broadcast(ServerMsg m)
        {
            for (int i = 0; i < 2; i++)
                if (_p[i]?.Conn != null) await _p[i].Conn.SendAsync(m);
        }

        async Task SendOther(int side, ServerMsg m)
        {
            int other = 1 - side;
            if (side >= 0 && _p[other]?.Conn != null) await _p[other].Conn.SendAsync(m);
        }

        void Finish()
        {
            if (_finished) return;
            _finished = true;
            _onDone?.Invoke(this);
        }

        static ServerMsg Err(string m) => new() { T = "error", Text = m };
        static string Trim(string s) => string.IsNullOrEmpty(s) ? "" : (s.Length > 200 ? s.Substring(0, 200) : s);

        // deterministic-ish seed without Date.Now in hot path; fine for battle RNG
        static ulong _seedCtr = 0x243F6A8885A308D3UL;
        static ulong NextSeed() => unchecked(_seedCtr = _seedCtr * 6364136223846793005UL + 1442695040888963407UL);
    }
}
