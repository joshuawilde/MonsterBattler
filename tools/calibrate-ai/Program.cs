using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MonsterBattler.Sim;
using MonsterBattler.Sim.Data;
using MonsterBattler.Sim.Tests; // TestData (headless dex/randbats loader)
using MonsterBattler.Game.AI;

// Self-play calibration: plays EloBattleAI at a range of softmax temperatures against each other in a
// round-robin, fits a relative Elo to each temperature from the win-rates, and writes the Elo<->temp
// table used by EloBattleAI.SetElo. "Elo" here is self-consistent (not real Showdown Elo).
class Program
{
    // Lower temperature = stronger (closer to argmax). Spread to cover random..optimal.
    static readonly float[] Temps = { 0.0f, 0.06f, 0.14f, 0.28f, 0.5f, 0.9f, 1.6f };

    static int Main(string[] args)
    {
        var dex = TestData.Dex;
        var randbats = TestData.Randbats;

        // `search [N]`: measure SearchAI vs the heuristic argmax ceiling (proves the ceiling rose).
        if (args.Length > 0 && args[0] == "search")
        {
            int games = args.Length > 1 && int.TryParse(args[1], out var gg) ? gg : 100;
            int depth = args.Length > 2 && int.TryParse(args[2], out var dd) ? dd : 2;
            RunSearchHeadToHead(dex, randbats, games, depth);
            return 0;
        }

        // `unified [perPair]`: ONE round-robin across heuristic-temperature AND search configs, fit a
        // single Elo scale, and print the full strength ladder (so the dial is smooth end to end).
        if (args.Length > 0 && args[0] == "unified")
        {
            int up = args.Length > 1 && int.TryParse(args[1], out var u) ? u : 50;
            RunUnified(dex, randbats, up);
            return 0;
        }

        int perPair = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 80;
        int n = Temps.Length;

        var W = new double[n, n]; // W[i,j] = score of i vs j (win=1, draw=0.5)
        var G = new double[n, n]; // games played i vs j
        ulong seed = 0xC0FFEE;

        Console.Error.WriteLine($"Calibrating {n} temperature buckets, {perPair} battles/pair " +
                                $"({n * (n - 1) / 2 * perPair} battles total)...");
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
            {
                for (int g = 0; g < perPair; g++)
                {
                    int res = PlayBattle(dex, randbats, Temps[i], Temps[j], seed++);
                    G[i, j]++; G[j, i]++;
                    if (res == 0) { W[i, j] += 1; }
                    else if (res == 1) { W[j, i] += 1; }
                    else { W[i, j] += 0.5; W[j, i] += 0.5; }
                }
                double wr = W[i, j] / G[i, j];
                Console.Error.WriteLine($"  T={Temps[i]:0.00} vs T={Temps[j]:0.00}: {wr * 100:0.0}% for stronger");
            }

        var elo = FitElo(n, W, G);

        // Pair (elo, temp), sort ascending by elo (temp decreases as elo rises).
        var rows = new List<(int elo, float t)>();
        for (int i = 0; i < n; i++) rows.Add(((int)Math.Round(elo[i]), Temps[i]));
        rows.Sort((a, b) => a.elo.CompareTo(b.elo));

        // Emit: human summary + JSON + ready-to-paste C# anchors.
        var sb = new StringBuilder();
        sb.AppendLine("// Calibrated Elo <-> temperature anchors (paste into EloBattleAI._curve):");
        sb.AppendLine("{");
        foreach (var r in rows) sb.AppendLine($"    ({r.elo}, {r.t:0.###}f),");
        sb.AppendLine("};");
        Console.WriteLine(sb.ToString());

        var json = new StringBuilder("{\n  \"anchors\": [\n");
        for (int i = 0; i < rows.Count; i++)
            json.Append($"    {{ \"elo\": {rows[i].elo}, \"t\": {rows[i].t:0.###} }}{(i < rows.Count - 1 ? "," : "")}\n");
        json.Append("  ]\n}\n");
        string outPath = Path.Combine(AppContext.BaseDirectory, "ai_calibration.json");
        File.WriteAllText(outPath, json.ToString());
        Console.Error.WriteLine($"Wrote {outPath}");
        return 0;
    }

    static IBattleAI EloT(float t, ulong s) { var a = new EloBattleAI(1000, s); a.SetTemperature(t); return a; }

    static void RunUnified(Dex dex, RandbatsDex randbats, int perPair)
    {
        var configs = new (string label, System.Func<ulong, IBattleAI> build)[]
        {
            ("heur T1.6",        s => EloT(1.6f, s)),
            ("heur T0.9",        s => EloT(0.9f, s)),
            ("heur T0.5",        s => EloT(0.5f, s)),
            ("heur T0.28",       s => EloT(0.28f, s)),
            ("heur T0.14",       s => EloT(0.14f, s)),
            ("heur argmax",      s => new HeuristicAI()),
            ("search d1 lite",   s => new SearchAI(depth: 1, topMy: 3, topOpp: 2, samples: 1, seed: s)),
            ("search d1",        s => new SearchAI(depth: 1, seed: s)),
            ("search d2",        s => new SearchAI(depth: 2, seed: s)),
        };
        int n = configs.Length;
        var W = new double[n, n];
        var G = new double[n, n];
        ulong seed = 0xCA11B;
        Console.Error.WriteLine($"Unified ladder: {n} configs, {perPair} battles/pair ({n * (n - 1) / 2 * perPair} total)...");
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
            {
                for (int g = 0; g < perPair; g++)
                {
                    var ai = configs[i].build(seed ^ 0xA);
                    var aj = configs[j].build(seed ^ 0xB);
                    int res = (g % 2 == 0) ? PlayBattleAI(dex, randbats, ai, aj, seed)
                                           : Flip(PlayBattleAI(dex, randbats, aj, ai, seed));
                    G[i, j]++; G[j, i]++;
                    if (res == 0) W[i, j]++; else if (res == 1) W[j, i]++; else { W[i, j] += 0.5; W[j, i] += 0.5; }
                    seed++;
                }
                Console.Error.WriteLine($"  {configs[i].label} vs {configs[j].label}: {W[i, j] / G[i, j] * 100:0.0}%");
            }

        var elo = FitElo(n, W, G);
        var rows = new List<(int elo, string label)>();
        for (int i = 0; i < n; i++) rows.Add(((int)System.Math.Round(elo[i]), configs[i].label));
        rows.Sort((a, b) => a.elo.CompareTo(b.elo));
        Console.WriteLine("\n// Unified strength ladder (single Elo scale):");
        foreach (var r in rows) Console.WriteLine($"//   {r.elo,5}  {r.label}");
    }

    static void RunSearchHeadToHead(Dex dex, RandbatsDex randbats, int games, int depth)
    {
        Console.Error.WriteLine($"SearchAI(depth={depth}) vs Heuristic-argmax ceiling, {games} battles...");
        double searchScore = 0;
        ulong seed = 0x5EA2C4;
        for (int g = 0; g < games; g++)
        {
            // Alternate sides so neither AI gets a fixed-side advantage.
            IBattleAI search = new SearchAI(depth: depth, seed: seed ^ 0x5E);
            IBattleAI heur = new HeuristicAI();
            int res = (g % 2 == 0)
                ? PlayBattleAI(dex, randbats, search, heur, seed)
                : Flip(PlayBattleAI(dex, randbats, heur, search, seed));
            if (res == 0) searchScore += 1; else if (res == -1) searchScore += 0.5;
            seed++;
            if ((g + 1) % 20 == 0) Console.Error.WriteLine($"  {g + 1}/{games}: search {searchScore}/{g + 1}");
        }
        double wr = searchScore / games;
        double elo = wr <= 0 ? -999 : wr >= 1 ? 999 : 400.0 * Math.Log10(wr / (1 - wr));
        Console.WriteLine($"SearchAI win rate vs argmax ceiling: {wr * 100:0.0}%  =>  ~{elo:+0;-0} Elo over the ~1350 ceiling");
    }

    static int Flip(int r) => r == 0 ? 1 : r == 1 ? 0 : -1;

    static int PlayBattle(Dex dex, RandbatsDex randbats, float tA, float tB, ulong seed)
    {
        var aiA = new EloBattleAI(1000, seed ^ 0x11); aiA.SetTemperature(tA);
        var aiB = new EloBattleAI(1000, seed ^ 0x22); aiB.SetTemperature(tB);
        return PlayBattleAI(dex, randbats, aiA, aiB, seed);
    }

    // Returns 0 if A wins, 1 if B wins, -1 draw.
    static int PlayBattleAI(Dex dex, RandbatsDex randbats, IBattleAI aiA, IBattleAI aiB, ulong seed)
    {
        var genPrng = new Prng(seed);
        var gen = new RandomTeamGenerator(dex, randbats, genPrng);
        var teamA = gen.GenerateTeam(6);
        var teamB = gen.GenerateTeam(6);

        var s0 = new Side { Name = "A" };
        s0.Team.AddRange(teamA); s0.ActiveSlots.Add(teamA[0]); teamA[0].IsActive = true;
        var s1 = new Side { Name = "B" };
        s1.Team.AddRange(teamB); s1.ActiveSlots.Add(teamB[0]); teamB[0].IsActive = true;

        var battle = new Battle(dex, seed ^ 0xABCDEF12);
        battle.Setup(s0, s1);

        int safety = 0;
        while (!battle.IsFinished && safety++ < 300)
        {
            ForceSwitchIfFainted(battle, 0);
            ForceSwitchIfFainted(battle, 1);
            if (battle.IsFinished) break;

            var cA = aiA.ChooseAction(battle, battle.Sides[0], battle.Sides[1]);
            var cB = aiB.ChooseAction(battle, battle.Sides[1], battle.Sides[0]);
            battle.Step(cA, cB);
        }

        if (battle.WinningSide.HasValue) return battle.WinningSide.Value; // 0 = A, 1 = B
        // Stalemate / safety cap: decide by remaining team HP fraction.
        return TeamHp(s0).CompareTo(TeamHp(s1)) switch { > 0 => 0, < 0 => 1, _ => -1 };
    }

    static void ForceSwitchIfFainted(Battle b, int side)
    {
        var s = b.Sides[side];
        if (s.ActiveSlots.Count == 0) return;
        var active = s.ActiveSlots[0];
        if (active == null || !active.IsFainted) return;
        for (int i = 0; i < s.Team.Count; i++)
        {
            var p = s.Team[i];
            if (p != null && p != active && !p.IsFainted) { b.Switch(s, i); return; }
        }
    }

    static double TeamHp(Side s)
    {
        double f = 0;
        foreach (var p in s.Team)
            if (p != null) f += p.MaxStats[(int)Stat.HP] > 0 ? (double)p.CurrentHp / p.MaxStats[(int)Stat.HP] : 0;
        return f;
    }

    // Fit a relative Elo per bucket from the pairwise score matrix (batched logistic gradient ascent).
    static double[] FitElo(int n, double[,] W, double[,] G)
    {
        var R = new double[n];
        for (int i = 0; i < n; i++) R[i] = 1000;
        for (int iter = 0; iter < 20000; iter++)
        {
            var delta = new double[n];
            for (int i = 0; i < n; i++)
            {
                double act = 0, exp = 0, games = 0;
                for (int j = 0; j < n; j++)
                {
                    if (i == j || G[i, j] == 0) continue;
                    double e = 1.0 / (1.0 + Math.Pow(10, (R[j] - R[i]) / 400.0));
                    act += W[i, j];
                    exp += G[i, j] * e;
                    games += G[i, j];
                }
                if (games > 0) delta[i] = 4.0 * (act - exp) / games;
            }
            for (int i = 0; i < n; i++) R[i] += delta[i];
        }
        double mean = 0; for (int i = 0; i < n; i++) mean += R[i]; mean /= n;
        for (int i = 0; i < n; i++) R[i] += 1200 - mean; // anchor mean to 1200
        return R;
    }
}
