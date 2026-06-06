using System.Linq;
using MonsterBattler.Sim;
using Xunit;

namespace MonsterBattler.Sim.Tests
{
    public class RandomTeamGeneratorTests
    {
        static RandomTeamGenerator Gen(ulong seed) =>
            new RandomTeamGenerator(TestData.Dex, TestData.Randbats, new Prng(seed));

        [Fact]
        public void GeneratesSixValidDistinctBattlers()
        {
            var team = Gen(12345).GenerateTeam();

            Assert.Equal(6, team.Count);
            Assert.Equal(6, team.Select(m => m.Species.Id).Distinct().Count()); // no duplicate species

            foreach (var mon in team)
            {
                Assert.InRange(mon.Moves.Count, 1, 4);
                Assert.Equal(mon.Moves.Count, mon.Moves.Select(s => s.Move.Id).Distinct().Count()); // no dup moves
                Assert.InRange(mon.Level, 1, 100);
                Assert.NotNull(mon.Ability);
                Assert.True(mon.MaxStats[(int)Stat.HP] > 0);
                Assert.Equal(mon.MaxStats[(int)Stat.HP], mon.CurrentHp);
                Assert.NotEqual(MonType.None, mon.TeraType);
            }
        }

        [Fact]
        public void IsDeterministicForSameSeed()
        {
            string Signature(ulong seed) =>
                string.Join(",", Gen(seed).GenerateTeam()
                    .Select(m => $"{m.Species.Id}:{string.Join("/", m.Moves.Select(s => s.Move.Id))}"));

            Assert.Equal(Signature(777), Signature(777));
            Assert.NotEqual(Signature(777), Signature(778));
        }

        [Fact]
        public void SpecialAttacker_ZeroesAttackEVs()
        {
            // Across a team, any mon whose moves are all special (no Atk-scaling physical move)
            // should have its Atk EVs trimmed to 0 (the documented PS spread behaviour).
            var team = Gen(2024).GenerateTeam();
            foreach (var mon in team)
            {
                bool hasPhysical = mon.Moves.Any(s =>
                    s.Move.Category == MoveCategory.Physical && s.Move.BasePower > 0);
                if (!hasPhysical)
                    Assert.Equal(0, mon.EVs[(int)Stat.Atk]);
            }
        }
    }
}
