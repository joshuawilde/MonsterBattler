using System.Linq;
using MonsterBattler.Sim;
using MonsterBattler.Sim.Events;
using Xunit;

namespace MonsterBattler.Sim.Tests
{
    /// <summary>
    /// Verifies Protosynthesis / Quark Drive / Booster Energy — the mechanic added this session that
    /// couldn't be observed firing in a random battle (RNG-dependent lead). These drive it directly.
    /// </summary>
    public class ParadoxBoostTests
    {
        [Fact]
        public void BoosterEnergy_ActivatesProtosynthesis_BoostsHighestAttackingStat()
        {
            // Great Tusk: HP115 / Atk131 / Def131 / SpA53 / SpD53 / Spe87 → highest non-HP stat is Atk.
            var tusk = TestBattlers.Make("greattusk", "protosynthesis", "boosterenergy");
            var battle = TestBattlers.SetupBattle(tusk, TestBattlers.Make("pikachu"));

            var slot = tusk.GetVolatile("paradoxboost");
            Assert.NotNull(slot);                                   // activated on switch-in
            Assert.Equal((int)Stat.Atk, slot.Counter);             // boosted stat = Atk
            Assert.Null(tusk.Item);                                 // Booster Energy consumed
            Assert.Contains(battle.Log.Lines, l => l.Contains("Booster Energy"));

            // Atk is multiplied ×1.3...
            var atk = new StatModifyEvent { Battle = battle, Owner = tusk, Stat = Stat.Atk, Value = 200 };
            battle.RunModifyAtk(atk);
            Assert.Equal(200 * 13 / 10, atk.Value);

            // ...but Def (not the chosen stat) is untouched.
            var def = new StatModifyEvent { Battle = battle, Owner = tusk, Stat = Stat.Def, Value = 200 };
            battle.RunModifyDef(def);
            Assert.Equal(200, def.Value);
        }

        [Fact]
        public void BoosterEnergy_SpeedHighest_GivesQuarkDrive15xSpeed()
        {
            // Iron Bundle: Spe 136 is its highest stat → Speed boost is ×1.5, not ×1.3.
            var bundle = TestBattlers.Make("ironbundle", "quarkdrive", "boosterenergy");
            var battle = TestBattlers.SetupBattle(bundle, TestBattlers.Make("pikachu"));

            var slot = bundle.GetVolatile("paradoxboost");
            Assert.NotNull(slot);
            Assert.Equal((int)Stat.Spe, slot.Counter);

            var spe = new StatModifyEvent { Battle = battle, Owner = bundle, Stat = Stat.Spe, Value = 200 };
            battle.RunModifySpe(spe);
            Assert.Equal(200 * 3 / 2, spe.Value);
        }

        [Fact]
        public void NoWeatherNoBooster_NoBoost()
        {
            // Protosynthesis with no sun and no Booster Energy must not activate.
            var tusk = TestBattlers.Make("greattusk", "protosynthesis");
            TestBattlers.SetupBattle(tusk, TestBattlers.Make("pikachu"));
            Assert.Null(tusk.GetVolatile("paradoxboost"));
        }

        [Fact]
        public void Protosynthesis_ActivatesInSun()
        {
            var tusk = TestBattlers.Make("greattusk", "protosynthesis");
            var foe = TestBattlers.Make("pikachu");
            // Put sun up before switch-in by building the battle then re-triggering switch-in under sun.
            var battle = new Battle(TestData.Dex, 1);
            var s0 = new Side { Name = "P" }; s0.Team.Add(tusk); s0.ActiveSlots.Add(tusk); tusk.IsActive = true;
            var s1 = new Side { Name = "O" }; s1.Team.Add(foe); s1.ActiveSlots.Add(foe); foe.IsActive = true;
            battle.Field.Weather = Weather.Sun;
            battle.Setup(s0, s1);

            var slot = tusk.GetVolatile("paradoxboost");
            Assert.NotNull(slot);
            Assert.Equal((int)Stat.Atk, slot.Counter);

            // And the boost lapses if the sun leaves (lazy re-check, source = "sun").
            battle.Field.Weather = Weather.None;
            var atk = new StatModifyEvent { Battle = battle, Owner = tusk, Stat = Stat.Atk, Value = 200 };
            battle.RunModifyAtk(atk);
            Assert.Equal(200, atk.Value);
        }
    }
}
