using System.Collections.Generic;
using MonsterBattler.Sim;
using MonsterBattler.Sim.Data;

namespace MonsterBattler.BattleServer
{
    /// <summary>Reconstructs a battle team from a wire <see cref="TeamSpec"/>, identically to the
    /// Unity client's NetTeamSpec.Build (BuildNamedTeam is deterministic per species).</summary>
    public static class TeamBuilder
    {
        public static List<Pokemon> Build(Dex dex, RandbatsDex randbats, TeamSpec spec)
        {
            var levels = new Dictionary<string, int>();
            for (int i = 0; i < spec.Species.Length; i++) levels[spec.Species[i]] = spec.Levels[i];
            var gen = new RandomTeamGenerator(dex, randbats, new Prng(1));
            var team = gen.BuildNamedTeam(spec.Species, id => levels.TryGetValue(id, out var l) ? l : -1);
            for (int i = 0; i < team.Count && i < spec.MovesCsv.Length; i++)
            {
                if (string.IsNullOrEmpty(spec.MovesCsv[i])) continue;
                team[i].Moves.Clear();
                foreach (var id in spec.MovesCsv[i].Split(','))
                    if (dex.Moves.TryGetValue(id, out var m))
                        team[i].Moves.Add(new MoveSlot { Move = m, Pp = m.Pp, MaxPp = m.Pp });
            }
            return team;
        }

        public static TeamSpec BotSpec(Dex dex, RandbatsDex randbats, int size, ulong seed, string username, int elo)
        {
            var team = new RandomTeamGenerator(dex, randbats, new Prng(seed)).GenerateTeam(System.Math.Max(1, size));
            var spec = new TeamSpec
            {
                Uid = "", Username = username, Elo = elo,
                Species = new string[team.Count], Levels = new int[team.Count], MovesCsv = new string[team.Count],
            };
            for (int i = 0; i < team.Count; i++)
            {
                spec.Species[i] = team[i].Species.Id;
                spec.Levels[i] = team[i].Level;
                var ids = new List<string>();
                foreach (var ms in team[i].Moves) ids.Add(ms.Move.Id);
                spec.MovesCsv[i] = string.Join(",", ids);
            }
            return spec;
        }
    }
}
