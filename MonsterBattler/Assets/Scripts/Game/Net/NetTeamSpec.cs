using System.Collections.Generic;
using MonsterBattler.Sim;
using MonsterBattler.Sim.Data;

namespace MonsterBattler.Game.Net
{
    /// <summary>
    /// Everything needed to reconstruct a player's battle team identically on any peer
    /// (both clients + the server): BuildNamedTeam is deterministic per species, so the
    /// roll (ability/item/IVs) needs no extra seed; equipped moves override the roll.
    /// </summary>
    public struct NetTeamSpec
    {
        public string Username;
        public int Elo;
        public string[] Species;
        public int[] Levels;
        public string[] MovesCsv;   // per mon: "moveid1,moveid2,…" — empty string keeps the rolled set

        /// <summary>Build the local player's spec from the meta save.</summary>
        public static NetTeamSpec FromMeta()
        {
            var team = Meta.MetaGame.BattleTeam() ?? new List<string>();
            var spec = new NetTeamSpec
            {
                Username = Meta.MetaGame.Profile.username,
                Elo = Meta.MetaGame.Profile.elo,
                Species = team.ToArray(),
                Levels = new int[team.Count],
                MovesCsv = new string[team.Count],
            };
            for (int i = 0; i < team.Count; i++)
            {
                spec.Levels[i] = Meta.MetaGame.CurrentLevel(team[i]);
                var equipped = Meta.MetaGame.EquippedMoveDatas(team[i]);
                spec.MovesCsv[i] = equipped is { Count: > 0 }
                    ? string.Join(",", equipped.ConvertAll(m => m.Id))
                    : "";
            }
            return spec;
        }

        /// <summary>Deterministically reconstruct the team this spec describes.</summary>
        public List<Pokemon> Build(Dex dex, RandbatsDex randbats)
        {
            var levels = new Dictionary<string, int>();
            for (int i = 0; i < Species.Length; i++) levels[Species[i]] = Levels[i];
            var gen = new RandomTeamGenerator(dex, randbats, new Prng(1));
            var team = gen.BuildNamedTeam(Species, id => levels.TryGetValue(id, out var l) ? l : -1);
            for (int i = 0; i < team.Count && i < MovesCsv.Length; i++)
            {
                if (string.IsNullOrEmpty(MovesCsv[i])) continue;
                team[i].Moves.Clear();
                foreach (var id in MovesCsv[i].Split(','))
                    if (dex.Moves.TryGetValue(id, out var m))
                        team[i].Moves.Add(new MoveSlot { Move = m, Pp = m.Pp, MaxPp = m.Pp });
            }
            return team;
        }
    }
}
