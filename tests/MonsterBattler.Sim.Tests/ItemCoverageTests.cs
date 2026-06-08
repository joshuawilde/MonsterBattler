using MonsterBattler.Sim;
using MonsterBattler.Sim.Events;
using Xunit;

namespace MonsterBattler.Sim.Tests
{
    /// <summary>Spot-checks for the newly-added held items (berries, sash, helmet, etc.).</summary>
    public class ItemCoverageTests
    {
        static readonly Choice Skip = new Choice { Kind = ChoiceKind.Skip };

        [Fact]
        public void SitrusBerry_HealsAtHalfHp()
        {
            var holder = TestBattlers.Make("blissey", "naturalcure", "sitrusberry");
            var b = TestBattlers.SetupBattle(TestBattlers.Make("snorlax"), holder);
            int max = holder.MaxStats[(int)Stat.HP];
            holder.CurrentHp = max * 2 / 5; // 40% → below the 50% threshold
            b.Step(Skip, Skip);             // end-of-turn residual eats the berry
            Assert.True(holder.CurrentHp > max * 2 / 5, "Sitrus should heal");
            Assert.Null(holder.Item);       // consumed
        }

        [Fact]
        public void FocusSash_SurvivesFullHpKO()
        {
            var holder = TestBattlers.Make("pikachu", "static", "focussash");
            var b = TestBattlers.SetupBattle(TestBattlers.Make("snorlax", "thickfat", null, 100, "tackle"), holder);
            int max = holder.MaxStats[(int)Stat.HP];
            var ev = new ModifyDamageEvent
            {
                Battle = b, User = b.Sides[0].ActiveSlots[0], Target = holder,
                Move = TestData.Dex.GetMove("tackle"), Damage = max + 50,
            };
            holder.ItemEffect.OnModifyDamage(ev, holder);
            Assert.Equal(max - 1, ev.Damage); // survives with 1 HP
            Assert.Null(holder.Item);          // sash consumed
        }

        [Fact]
        public void RockyHelmet_ChipsContactAttacker()
        {
            var holder = TestBattlers.Make("snorlax", "thickfat", "rockyhelmet");
            var atk = TestBattlers.Make("lucario", "innerfocus", null, 100, "closecombat");
            var b = TestBattlers.SetupBattle(atk, holder);
            int atkMax = atk.MaxStats[(int)Stat.HP];
            b.Step(Choice.UseMove("closecombat"), Skip);
            Assert.True(atk.CurrentHp <= atkMax - atkMax / 6 + 1, "contact attacker should take Rocky Helmet chip");
        }
    }
}
