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
        public void InfoText_ShowsBoostedStatsWithStage()
        {
            var mon = TestBattlers.Make("lucario", "innerfocus", null, 100, "closecombat");
            int baseAtk = mon.MaxStats[(int)Stat.Atk];
            mon.StatStages[(int)Stat.Atk] = 2; // +2 Attack (e.g. after Swords Dance)

            var text = PokemonInfoText.TopBodyText(mon);

            int boosted = (int)(baseAtk * Stats.StageMult(2)); // ×2 at +2
            Assert.Contains($"{boosted} (×2)", text);          // shows the boosted value + multiplier
            Assert.DoesNotContain($"<b>Atk</b> {baseAtk} ", text); // not the raw base
        }

        [Fact]
        public void InfoText_HeaderBodyAndTypesCoverStatsAbilityItemMoves()
        {
            var mon = TestBattlers.Make("charizard", "blaze", "heavydutyboots", 100, "flamethrower", "earthquake");
            // Resolve ability/item effects + descriptions the way a real battle does.
            TestBattlers.SetupBattle(mon, TestBattlers.Make("pikachu"));

            Assert.Contains("Charizard", PokemonInfoText.HeaderText(mon));

            Assert.Contains("Atk", PokemonInfoText.TopBodyText(mon));   // stats (top block)
            var bottom = PokemonInfoText.BottomBodyText(mon);
            Assert.Contains("Blaze", bottom);                          // ability
            Assert.Contains("Heavy-Duty Boots", bottom);              // item
            Assert.Contains("10% chance to burn the target.", bottom); // Flamethrower description

            // Types/matchup are chips now, not text — verify the data feeding them.
            var types = PokemonInfoText.EffectiveTypes(mon);
            Assert.Contains(MonType.Fire, types);
            Assert.Contains(MonType.Flying, types);
        }
    }
}
