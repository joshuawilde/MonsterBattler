using MonsterBattler.Game;
using MonsterBattler.Sim;
using Xunit;

namespace MonsterBattler.Sim.Tests
{
    public class PokemonInfoTests
    {
        [Fact]
        public void TypeMatchup_Charizard_HasQuadRockWeaknessAndGroundImmunity()
        {
            // Charizard is Fire/Flying: ×4 Rock, ×0 Ground (Flying immunity), resists Grass/Fighting/etc.
            var m = TypeMatchup.Defensive(MonType.Fire, MonType.Flying);
            Assert.Contains(m, e => e.Type == MonType.Rock && e.Multiplier == 4f);
            Assert.Contains(m, e => e.Type == MonType.Ground && e.Multiplier == 0f);
            Assert.Contains(m, e => e.Type == MonType.Grass && e.Multiplier == 0.25f);
            // Sorted weaknesses-first, immunities-last.
            Assert.Equal(4f, m[0].Multiplier);
            Assert.Equal(0f, m[^1].Multiplier);
            // Neutral types are omitted.
            Assert.DoesNotContain(m, e => e.Multiplier == 1f);
        }

        [Fact]
        public void InfoText_IncludesStatsAbilityItemMatchupAndMoveDescriptions()
        {
            var mon = TestBattlers.Make("charizard", "blaze", "heavydutyboots", 100, "flamethrower", "earthquake");
            // Resolve ability/item effects + descriptions the way a real battle does.
            TestBattlers.SetupBattle(mon, TestBattlers.Make("pikachu"));

            var text = PokemonInfoText.Build(mon);

            Assert.Contains("Charizard", text);
            Assert.Contains("Fire/Flying", text);
            Assert.Contains("Atk", text);                        // stats line
            Assert.Contains("Ability: Blaze", text);
            Assert.Contains("Item: Heavy-Duty Boots", text);
            Assert.Contains("Rock ×4", text);                    // weakness
            Assert.Contains("Ground", text);                     // immunity listed
            Assert.Contains("10% chance to burn the target.", text); // Flamethrower move description
        }
    }
}
