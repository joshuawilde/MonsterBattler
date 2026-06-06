using System.Linq;
using MonsterBattler.Sim;
using Xunit;

namespace MonsterBattler.Sim.Tests
{
    /// <summary>
    /// Validates the contract the UI's turn-playback relies on: a resolved turn leaves an ordered
    /// log where the move comes before its damage, and damage lines carry a parseable "cur/max" HP
    /// snapshot (BattleView animates each bar to that value as it replays the log).
    /// </summary>
    public class TurnLogTests
    {
        [Fact]
        public void Turn_ProducesOrderedMoveThenDamageWithHpSnapshot()
        {
            var attacker = TestBattlers.Make("pikachu", "static", null, 100, "thunderbolt");
            var target = TestBattlers.Make("blissey", "naturalcure", null, 100, "softboiled");
            var battle = TestBattlers.SetupBattle(attacker, target);

            battle.Log.Lines.Clear();
            battle.Step(Choice.UseMove("thunderbolt"), new Choice { Kind = ChoiceKind.Skip });
            var log = battle.Log.Lines;

            int moveIdx = log.FindIndex(l => l.StartsWith("|move|Pikachu|Thunderbolt"));
            int dmgIdx = log.FindIndex(l => l.StartsWith("|-damage|Blissey|"));
            Assert.True(moveIdx >= 0, "expected a |move| line for Thunderbolt");
            Assert.True(dmgIdx > moveIdx, "damage should be logged after the move");

            // The damage line must carry a parseable cur/max snapshot.
            var parts = log[dmgIdx].Split('|'); // ["", "-damage", "Blissey", "cur/max", ...]
            var hp = parts[3].Split('/');
            Assert.True(int.TryParse(hp[0], out int cur), "current HP should parse");
            Assert.True(int.TryParse(hp[1], out int max), "max HP should parse");
            Assert.True(cur < max, "Blissey should have taken damage");
        }
    }
}
