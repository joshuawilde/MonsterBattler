using MonsterBattler.Sim;
using Xunit;

namespace MonsterBattler.Sim.Tests
{
    /// <summary>
    /// Coverage for status moves wired purely through data (selfBoosts) or by reusing existing
    /// effects (recovermove / synthesismove) — i.e. no new effect class needed.
    /// </summary>
    public class StatusMoveCoverageTests
    {
        static readonly Choice Skip = new Choice { Kind = ChoiceKind.Skip };

        static Battle Use(Pokemon user, string moveId, Pokemon target = null)
        {
            var b = TestBattlers.SetupBattle(user, target ?? TestBattlers.Make("blissey"));
            b.Step(Choice.UseMove(moveId), Skip);
            return b;
        }

        [Fact]
        public void QuiverDance_RaisesSpaSpdSpe()
        {
            var u = TestBattlers.Make("volcarona", "flamebody", null, 100, "quiverdance");
            Use(u, "quiverdance");
            Assert.Equal(1, u.StatStages[(int)Stat.SpA]);
            Assert.Equal(1, u.StatStages[(int)Stat.SpD]);
            Assert.Equal(1, u.StatStages[(int)Stat.Spe]);
        }

        [Fact]
        public void Agility_RaisesSpeedByTwo()
        {
            var u = TestBattlers.Make("dragapult", "clearbody", null, 100, "agility");
            Use(u, "agility");
            Assert.Equal(2, u.StatStages[(int)Stat.Spe]);
        }

        [Fact]
        public void SlackOff_HealsAboutHalf()
        {
            var u = TestBattlers.Make("slowbro", "owntempo", null, 100, "slackoff");
            int max = u.MaxStats[(int)Stat.HP];
            u.CurrentHp = max / 4;
            Use(u, "slackoff");
            Assert.True(u.CurrentHp >= max / 4 + max / 2 - 2, $"expected ~half heal, hp={u.CurrentHp}/{max}");
        }

        [Fact]
        public void Glare_ParalyzesTarget()
        {
            var target = TestBattlers.Make("blissey", "naturalcure");
            Use(TestBattlers.Make("serperior", "overgrow", null, 100, "glare"), "glare", target);
            Assert.Equal(StatusCondition.Paralysis, target.Status);
        }

        [Fact]
        public void Spore_SleepsTarget() // Spore = 100% accuracy, same sleepmove effect as Hypnosis
        {
            var target = TestBattlers.Make("blissey", "naturalcure");
            Use(TestBattlers.Make("breloom", "technician", null, 100, "spore"), "spore", target);
            Assert.Equal(StatusCondition.Sleep, target.Status);
        }

        [Fact]
        public void RainDance_SetsRain()
        {
            var u = TestBattlers.Make("pelipper", "drizzle", null, 100, "raindance");
            var b = Use(u, "raindance");
            Assert.Equal(Weather.Rain, b.Field.Weather);
        }

        [Fact]
        public void Haze_ClearsBothSidesBoosts()
        {
            var u = TestBattlers.Make("toxapex", "regenerator", null, 100, "haze");
            var foe = TestBattlers.Make("blissey");
            u.StatStages[(int)Stat.Atk] = 2;
            foe.StatStages[(int)Stat.Def] = -1;
            Use(u, "haze", foe);
            Assert.Equal(0, u.StatStages[(int)Stat.Atk]);
            Assert.Equal(0, foe.StatStages[(int)Stat.Def]);
        }

        [Fact]
        public void Rest_FullHealsAndSleeps()
        {
            var u = TestBattlers.Make("snorlax", "thickfat", null, 100, "rest");
            int max = u.MaxStats[(int)Stat.HP];
            u.CurrentHp = max / 2;
            Use(u, "rest");
            Assert.Equal(max, u.CurrentHp);
            Assert.Equal(StatusCondition.Sleep, u.Status);
        }

        [Fact]
        public void HealBell_CuresUserStatus()
        {
            var u = TestBattlers.Make("blissey", "naturalcure", null, 100, "healbell");
            u.Status = StatusCondition.Burn;
            Use(u, "healbell");
            Assert.Equal(StatusCondition.None, u.Status);
        }

        [Fact]
        public void Taunt_AppliesVolatileToTarget()
        {
            var foe = TestBattlers.Make("blissey", "naturalcure");
            Use(TestBattlers.Make("thundurus", "prankster", null, 100, "taunt"), "taunt", foe);
            Assert.NotNull(foe.GetVolatile("taunt"));
        }
    }
}
