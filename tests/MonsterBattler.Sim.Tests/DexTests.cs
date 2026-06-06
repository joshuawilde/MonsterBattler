using MonsterBattler.Sim.Data;
using Xunit;

namespace MonsterBattler.Sim.Tests
{
    public class DexTests
    {
        [Fact]
        public void LoadsFullGen9Dex()
        {
            var dex = TestData.Dex;
            Assert.True(dex.Species.Count > 800, $"expected >800 species, got {dex.Species.Count}");
            Assert.True(dex.Moves.Count > 600, $"expected >600 moves, got {dex.Moves.Count}");
            Assert.True(dex.Abilities.Count > 250, $"expected >250 abilities, got {dex.Abilities.Count}");
        }

        [Fact]
        public void KnownSpeciesHasCorrectData()
        {
            var tusk = TestData.Dex.Get("greattusk");
            Assert.Equal("Great Tusk", tusk.Name);
            Assert.Equal(MonType.Ground, tusk.Type1);
            Assert.Equal(MonType.Fighting, tusk.Type2);
            Assert.Equal(115, tusk.BaseStats.HP);
            Assert.Equal(131, tusk.BaseStats.Atk);
            Assert.Contains("protosynthesis", tusk.AbilityIds);
        }

        [Fact]
        public void RandbatsDataResolvesAgainstDex()
        {
            var dex = TestData.Dex;
            var rb = TestData.Randbats;
            Assert.True(rb.Species.Count > 400, $"expected >400 randbats species, got {rb.Species.Count}");
            foreach (var (id, entry) in rb.Species)
            {
                Assert.True(dex.Species.ContainsKey(id), $"randbats species '{id}' missing from dex");
                foreach (var set in entry.Sets)
                    foreach (var moveId in set.MovepoolIds)
                        Assert.True(dex.Moves.ContainsKey(moveId), $"{id}: move '{moveId}' missing from dex");
            }
        }
    }
}
