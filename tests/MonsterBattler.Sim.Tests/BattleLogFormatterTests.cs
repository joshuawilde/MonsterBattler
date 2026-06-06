using MonsterBattler.Game;
using Xunit;

namespace MonsterBattler.Sim.Tests
{
    /// <summary>
    /// Verifies the protocol→readable translation that drives the battle-log feed. Pure string
    /// work, so we can assert exact wording without running a battle or touching Unity.
    /// </summary>
    public class BattleLogFormatterTests
    {
        [Theory]
        [InlineData("|move|Pikachu|Thunderbolt|Raichu", "Pikachu used Thunderbolt!")]
        [InlineData("|switch|Gogoat|Gogoat|360/360", "Gogoat was sent out!")]
        [InlineData("|faint|Pikachu", "Pikachu fainted!")]
        [InlineData("|turn|3", "— Turn 3 —")]
        [InlineData("|-crit|Pikachu", "A critical hit!")]
        [InlineData("|-supereffective|Pikachu", null)]                 // unmapped → skipped
        [InlineData("|-status|Snorlax|brn", "Snorlax was burned!")]
        [InlineData("|-status|Snorlax|slp", "Snorlax fell asleep!")]
        [InlineData("|-curestatus|Snorlax|slp", "Snorlax woke up!")]
        [InlineData("|-boost|Gogoat|atk|2", "Gogoat's Attack rose sharply!")]
        [InlineData("|-unboost|Gogoat|spe|1", "Gogoat's Speed fell!")]
        [InlineData("|-terastallize|Gogoat|Grass", "Gogoat Terastallized into Grass!")]
        [InlineData("|cant|Pikachu|flinch", "Pikachu flinched and couldn't move!")]
        [InlineData("|-weather|none|[from] ability: Cloud Nine", "The weather cleared.")]
        public void Translates(string protocol, string expected)
        {
            Assert.Equal(expected, BattleLogFormatter.Format(protocol));
        }

        [Fact]
        public void DamageFromResidualSource_ReadsNaturally()
        {
            Assert.Equal("Snorlax was hurt by its burn!",
                BattleLogFormatter.Format("|-damage|Snorlax|120/200|[from] brn"));
            Assert.Equal("Charizard was hurt by Stealth Rock!",
                BattleLogFormatter.Format("|-damage|Charizard|50/100|[from] Stealth Rock"));
        }

        [Fact]
        public void DirectDamage_IsSkipped_HpBarConveysIt()
        {
            // Plain move damage (no [from]) shouldn't spam the feed — the HP bar shows it.
            Assert.Null(BattleLogFormatter.Format("|-damage|Pikachu|150/200"));
        }

        [Fact]
        public void HealFromItem_NamesTheSource()
        {
            Assert.Equal("Snorlax restored HP (Leftovers).",
                BattleLogFormatter.Format("|-heal|Snorlax|180/200|[from] item: Leftovers"));
        }

        [Fact]
        public void EnditemKnockedOff_ReadsAsKnockedOff()
        {
            Assert.Equal("Gengar's Leftovers was knocked off!",
                BattleLogFormatter.Format("|-enditem|Gengar|Leftovers|[from] move: Knock Off"));
        }
    }
}
