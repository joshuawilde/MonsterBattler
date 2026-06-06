using MonsterBattler.Sim;
using MonsterBattler.Sim.Events;
using Xunit;

namespace MonsterBattler.Sim.Tests
{
    public class ItemEffectTests
    {
        [Fact]
        public void AssaultVest_BoostsSpD_AndBlocksStatusMoves()
        {
            var mon = TestBattlers.Make("snorlax", "thickfat", "assaultvest", 100, "earthquake", "toxic");
            var battle = TestBattlers.SetupBattle(mon, TestBattlers.Make("pikachu"));

            // ×1.5 Special Defense.
            var spd = new StatModifyEvent { Battle = battle, Owner = mon, Stat = Stat.SpD, Value = 200 };
            battle.RunModifySpD(spd);
            Assert.Equal(200 * 3 / 2, spd.Value);

            // Status move is blocked...
            var status = new BeforeMoveEvent { Battle = battle, User = mon, Move = TestData.Dex.GetMove("toxic") };
            battle.RunBeforeMove(status);
            Assert.True(status.Cancelled);

            // ...attacking move is allowed.
            var attack = new BeforeMoveEvent { Battle = battle, User = mon, Move = TestData.Dex.GetMove("earthquake") };
            battle.RunBeforeMove(attack);
            Assert.False(attack.Cancelled);
        }

        [Fact]
        public void HeavyDutyBoots_NegatesStealthRock()
        {
            // Charizard is 4× weak to Rock — without boots, Stealth Rock bites hard.
            var withBoots = SwitchIntoStealthRock("heavydutyboots");
            Assert.Equal(withBoots.MaxStats[(int)Stat.HP], withBoots.CurrentHp); // unscathed

            var noBoots = SwitchIntoStealthRock(null);
            Assert.True(noBoots.CurrentHp < noBoots.MaxStats[(int)Stat.HP]);     // took hazard damage
        }

        static Pokemon SwitchIntoStealthRock(string item)
        {
            var charizard = TestBattlers.Make("charizard", "blaze", item);
            var battle = TestBattlers.SetupBattle(charizard, TestBattlers.Make("pikachu"));
            // Lay rocks on the charizard's side, then simulate it switching in.
            battle.AddSideCondition(battle.Sides[0], "stealthrock", maxLayers: 1);
            charizard.CurrentHp = charizard.MaxStats[(int)Stat.HP]; // reset to full before the switch
            battle.RunSwitchIn(new SwitchInEvent { Battle = battle, Pokemon = charizard });
            return charizard;
        }
    }
}
