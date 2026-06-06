using MonsterBattler.Sim;
using MonsterBattler.Sim.Events;
using Xunit;

namespace MonsterBattler.Sim.Tests
{
    /// <summary>
    /// Verifies the chance-based secondary-effect system and the abilities that modify it.
    /// Uses Nuzzle (a guaranteed 100% paralysis secondary) so rolls are deterministic, and
    /// Close Combat (a guaranteed self stat drop) for the move.self path.
    /// </summary>
    public class SecondaryEffectTests
    {
        static readonly Choice Skip = new Choice { Kind = ChoiceKind.Skip };

        // Run a single turn where `attacker` uses `moveId` and the target does nothing.
        static Battle UseMove(Pokemon attacker, Pokemon target, string moveId)
        {
            var b = TestBattlers.SetupBattle(attacker, target);
            b.Step(Choice.UseMove(moveId), Skip);
            return b;
        }

        [Fact]
        public void Nuzzle_AlwaysParalyzesTarget()
        {
            var target = TestBattlers.Make("blissey", "thickfat");
            UseMove(TestBattlers.Make("pikachu", "static", null, 100, "nuzzle"), target, "nuzzle");
            Assert.Equal(StatusCondition.Paralysis, target.Status);
        }

        [Fact]
        public void ShieldDust_BlocksTheParalysis()
        {
            var target = TestBattlers.Make("blissey", "shielddust");
            UseMove(TestBattlers.Make("pikachu", "static", null, 100, "nuzzle"), target, "nuzzle");
            Assert.Equal(StatusCondition.None, target.Status);
        }

        [Fact]
        public void SheerForce_SuppressesTheSecondary()
        {
            var target = TestBattlers.Make("blissey", "thickfat");
            UseMove(TestBattlers.Make("pikachu", "sheerforce", null, 100, "nuzzle"), target, "nuzzle");
            Assert.Equal(StatusCondition.None, target.Status);
        }

        [Fact]
        public void SheerForce_BoostsBasePowerOfMovesWithSecondaries()
        {
            var user = TestBattlers.Make("pikachu", "sheerforce", null, 100, "nuzzle");
            var b = TestBattlers.SetupBattle(user, TestBattlers.Make("blissey"));
            var move = TestData.Dex.GetMove("nuzzle"); // 20 BP, has a secondary
            var ev = new BasePowerEvent { Battle = b, User = user, Target = b.Sides[1].ActiveSlots[0], Move = move, BasePower = move.BasePower };
            b.RunBasePower(ev);
            Assert.Equal(20 * 13 / 10, ev.BasePower); // 26
        }

        [Fact]
        public void SheerForce_DoesNotBoostMovesWithoutSecondaries()
        {
            var user = TestBattlers.Make("pikachu", "sheerforce", null, 100, "earthquake");
            var b = TestBattlers.SetupBattle(user, TestBattlers.Make("blissey"));
            var move = TestData.Dex.GetMove("earthquake"); // no secondary
            var ev = new BasePowerEvent { Battle = b, User = user, Target = b.Sides[1].ActiveSlots[0], Move = move, BasePower = move.BasePower };
            b.RunBasePower(ev);
            Assert.Equal(move.BasePower, ev.BasePower);
        }

        [Fact]
        public void CloseCombat_DropsUsersOwnDefenses()
        {
            var user = TestBattlers.Make("lucario", "innerfocus", null, 100, "closecombat");
            UseMove(user, TestBattlers.Make("blissey"), "closecombat");
            Assert.Equal(-1, user.StatStages[(int)Stat.Def]);
            Assert.Equal(-1, user.StatStages[(int)Stat.SpD]);
        }

        [Fact]
        public void DataImport_AttachedSecondaries()
        {
            // Sanity: the importer populated the new fields the system reads.
            Assert.NotNull(TestData.Dex.GetMove("flamethrower").Secondaries);   // 10% burn
            Assert.NotNull(TestData.Dex.GetMove("closecombat").SelfBoosts);     // -Def/-SpD
            Assert.Null(TestData.Dex.GetMove("tackle").Secondaries);            // none
        }
    }
}
