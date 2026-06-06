using MonsterBattler.Sim;
using Xunit;

namespace MonsterBattler.Sim.Tests
{
    /// <summary>
    /// Regression: a volatile/side-condition handler that adds or removes a volatile while the
    /// engine is dispatching an event over a Pokemon's effects used to throw
    /// "Collection was modified" because the dispatch iterated the live volatiles dictionary.
    /// </summary>
    public class DispatchMutationTests
    {
        [Fact]
        public void ExpiringConfusion_RemovingItselfDuringBeforeMove_DoesNotThrow()
        {
            var mon = TestBattlers.Make("pikachu", "static", null, 100, "thunderbolt");
            var battle = TestBattlers.SetupBattle(mon, TestBattlers.Make("blissey", "thickfat"));

            // Confusion with no turns left removes itself on the next OnBeforeMove — mid-dispatch.
            var slot = battle.AddVolatile(mon, "confusion");
            slot.Turns = 0;

            var ex = Record.Exception(() =>
                battle.Step(Choice.UseMove("thunderbolt"), new Choice { Kind = ChoiceKind.Skip }));

            Assert.Null(ex);                                 // no "Collection was modified"
            Assert.Null(mon.GetVolatile("confusion"));       // it expired and was removed cleanly
        }

        [Fact]
        public void ToxicSpikes_AbsorbedByPoisonType_DoesNotThrow()
        {
            // A Poison type switching in removes Toxic Spikes during the switch-in dispatch.
            var poison = TestBattlers.Make("muk", "poisontouch");        // Poison type
            var battle = TestBattlers.SetupBattle(TestBattlers.Make("pikachu"), poison);
            battle.AddSideCondition(battle.Sides[1], "toxicspikes", maxLayers: 2);

            var ex = Record.Exception(() =>
                battle.RunSwitchIn(new Events.SwitchInEvent { Battle = battle, Pokemon = poison }));

            Assert.Null(ex);
            Assert.False(battle.Sides[1].Conditions.ContainsKey("toxicspikes")); // absorbed
        }
    }
}
