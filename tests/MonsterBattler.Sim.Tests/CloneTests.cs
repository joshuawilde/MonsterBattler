using System.Text;
using MonsterBattler.Sim;
using Xunit;

namespace MonsterBattler.Sim.Tests
{
    /// <summary>
    /// Validates Battle.Clone: a clone must (1) step to the SAME result as the original given the same
    /// choices + copied PRNG, and (2) never mutate the original when the clone is stepped (no aliasing).
    /// These two properties are what make the clone safe for AI search.
    /// </summary>
    public class CloneTests
    {
        static Battle FreshBattle(ulong seed)
        {
            var gen = new RandomTeamGenerator(TestData.Dex, TestData.Randbats, new Prng(seed));
            var teamA = gen.GenerateTeam(6);
            var teamB = gen.GenerateTeam(6);
            var s0 = new Side { Name = "A" };
            s0.Team.AddRange(teamA); s0.ActiveSlots.Add(teamA[0]); teamA[0].IsActive = true;
            var s1 = new Side { Name = "B" };
            s1.Team.AddRange(teamB); s1.ActiveSlots.Add(teamB[0]); teamB[0].IsActive = true;
            var b = new Battle(TestData.Dex, seed ^ 0xDEAD);
            b.Setup(s0, s1);
            return b;
        }

        static Choice Pick(Side side)
        {
            var a = side.ActiveSlots[0];
            if (a == null || a.IsFainted) return Choice.UseMove("tackle");
            foreach (var m in a.Moves) if (!m.Disabled && m.Pp > 0) return Choice.UseMove(m.Move.Id);
            return Choice.UseMove(a.Moves.Count > 0 ? a.Moves[0].Move.Id : "tackle");
        }

        static void ForceSwitch(Battle b)
        {
            foreach (var side in b.Sides)
            {
                if (side.ActiveSlots.Count == 0) continue;
                var act = side.ActiveSlots[0];
                if (act == null || !act.IsFainted) continue;
                for (int i = 0; i < side.Team.Count; i++)
                    if (side.Team[i] != null && side.Team[i] != act && !side.Team[i].IsFainted) { b.Switch(side, i); break; }
            }
        }

        static void Advance(Battle b, int turns)
        {
            for (int t = 0; t < turns && !b.IsFinished; t++)
            {
                ForceSwitch(b);
                if (b.IsFinished) break;
                b.Step(Pick(b.Sides[0]), Pick(b.Sides[1]));
            }
        }

        static string Snapshot(Battle b)
        {
            var sb = new StringBuilder();
            sb.Append($"turn={b.TurnNumber};fin={b.IsFinished};win={b.WinningSide};");
            sb.Append($"weather={b.Field.Weather}/{b.Field.WeatherTurnsLeft};terrain={b.Field.Terrain}/{b.Field.TerrainTurnsLeft};");
            for (int s = 0; s < 2; s++)
            {
                foreach (var m in b.Sides[s].Team)
                {
                    sb.Append($"[{m.Species.Id} hp={m.CurrentHp} st={m.Status} tox={m.ToxicCounter} ");
                    for (int i = 1; i <= 5; i++) sb.Append(m.StatStages[i]);
                    sb.Append(" vol=");
                    foreach (var v in m.Volatiles.Keys) sb.Append(v + ",");
                    sb.Append("] ");
                }
            }
            return sb.ToString();
        }

        [Fact]
        public void Clone_StepsToSameResultAsOriginal()
        {
            for (ulong seed = 1; seed <= 6; seed++)
            {
                var orig = FreshBattle(seed);
                Advance(orig, 3); // build up some non-trivial state
                if (orig.IsFinished) continue;

                var clone = orig.Clone(); // copies PRNG state → same rolls
                var c0 = Pick(orig.Sides[0]);
                var c1 = Pick(orig.Sides[1]);

                orig.Step(c0, c1);
                clone.Step(c0, c1);

                Assert.Equal(Snapshot(orig), Snapshot(clone));
            }
        }

        [Fact]
        public void Clone_SteppingCloneDoesNotMutateOriginal()
        {
            for (ulong seed = 1; seed <= 6; seed++)
            {
                var orig = FreshBattle(seed);
                Advance(orig, 3);
                if (orig.IsFinished) continue;

                var clone = orig.Clone();
                string before = Snapshot(orig);
                clone.Step(Pick(clone.Sides[0]), Pick(clone.Sides[1])); // mutate the clone only
                Assert.Equal(before, Snapshot(orig)); // original untouched
            }
        }
    }
}
