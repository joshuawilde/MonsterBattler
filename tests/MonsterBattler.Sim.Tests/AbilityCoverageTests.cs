using MonsterBattler.Sim;
using MonsterBattler.Sim.Events;
using Xunit;

namespace MonsterBattler.Sim.Tests
{
    /// <summary>Spot-checks for newly-added abilities, incl. the on-KO hook (Moxie).</summary>
    public class AbilityCoverageTests
    {
        static readonly Choice Skip = new Choice { Kind = ChoiceKind.Skip };

        [Fact]
        public void Moxie_RaisesAttackOnKO()
        {
            var atk = TestBattlers.Make("krookodile", "moxie", null, 100, "earthquake");
            var foe = TestBattlers.Make("blissey", "naturalcure");
            var b = TestBattlers.SetupBattle(atk, foe);
            foe.CurrentHp = 1; // ensure the hit KOs
            b.Step(Choice.UseMove("earthquake"), Skip);
            Assert.True(foe.IsFainted);
            Assert.Equal(1, atk.StatStages[(int)Stat.Atk]);
        }

        [Fact]
        public void Technician_BoostsWeakMoveBasePower()
        {
            var u = TestBattlers.Make("scizor", "technician", null, 100, "bulletpunch"); // 40 BP
            var b = TestBattlers.SetupBattle(u, TestBattlers.Make("blissey"));
            var move = TestData.Dex.GetMove("bulletpunch");
            var ev = new BasePowerEvent { Battle = b, User = u, Target = b.Sides[1].ActiveSlots[0], Move = move, BasePower = move.BasePower };
            b.RunBasePower(ev);
            Assert.Equal(move.BasePower * 3 / 2, ev.BasePower);
        }

        [Fact]
        public void Stamina_RaisesDefenseWhenHit()
        {
            var def = TestBattlers.Make("mudsdale", "stamina", null, 100);
            var atk = TestBattlers.Make("pikachu", "static", null, 100, "tackle");
            var b = TestBattlers.SetupBattle(atk, def);
            b.Step(Choice.UseMove("tackle"), Skip);
            Assert.True(def.StatStages[(int)Stat.Def] >= 1);
        }

        [Fact]
        public void Protean_TurnsUserIntoMoveType()
        {
            var u = TestBattlers.Make("greninja", "protean", null, 100, "watergun");
            TestBattlers.SetupBattle(u, TestBattlers.Make("blissey")).Step(Choice.UseMove("watergun"), Skip);
            var (t1, _) = u.CurrentTypes();
            Assert.Equal(MonType.Water, t1);
        }

        [Fact]
        public void MoldBreaker_IgnoresLevitateImmunity()
        {
            var levit = TestBattlers.Make("bronzong", "levitate"); // normally immune to Ground
            var atk = TestBattlers.Make("excadrill", "moldbreaker", null, 100, "earthquake");
            var b = TestBattlers.SetupBattle(atk, levit);
            int before = levit.CurrentHp;
            b.Step(Choice.UseMove("earthquake"), Skip);
            Assert.True(levit.CurrentHp < before, "Mold Breaker should let Earthquake hit a Levitate mon");
        }

        [Fact]
        public void ShadowTag_TrapsOpponent()
        {
            var trapped = TestBattlers.Make("blissey", "naturalcure"); // not Ghost
            var b = TestBattlers.SetupBattle(trapped, TestBattlers.Make("gothitelle", "shadowtag"));
            Assert.True(b.IsTrapped(trapped));
        }

        [Fact]
        public void LowKick_ScalesWithTargetWeight()
        {
            var u = TestBattlers.Make("lucario", "innerfocus", null, 100, "lowkick");
            var heavy = TestBattlers.Make("snorlax", "thickfat"); // 460 kg
            var b = TestBattlers.SetupBattle(u, heavy);
            var eff = MonsterBattler.Sim.Effects.EffectRegistry.Get("lowkickmove");
            var ev = new BasePowerEvent { Battle = b, User = u, Target = heavy, Move = TestData.Dex.GetMove("lowkick"), BasePower = 1 };
            eff.OnBasePower(ev, null);
            Assert.Equal(120, ev.BasePower); // ≥200 kg → 120 BP
        }

        [Fact]
        public void MagicBounce_ReflectsStatusMove()
        {
            var bouncer = TestBattlers.Make("hatterene", "magicbounce");
            var foe = TestBattlers.Make("blissey", "naturalcure", null, 100, "thunderwave");
            var b = TestBattlers.SetupBattle(foe, bouncer);
            b.Step(Choice.UseMove("thunderwave"), Skip);
            Assert.Equal(StatusCondition.Paralysis, foe.Status); // bounced back onto the user
            Assert.Equal(StatusCondition.None, bouncer.Status);
        }

        [Fact]
        public void Imposter_CopiesTheFoe()
        {
            var ditto = TestBattlers.Make("ditto", "imposter");
            var foe = TestBattlers.Make("dragonite", "multiscale");
            TestBattlers.SetupBattle(ditto, foe); // switch-in fires at setup
            Assert.Equal(foe.Species.Id, ditto.Species.Id);
        }

        [Fact]
        public void PivotMove_HonorsChosenIncomingMon()
        {
            var user = TestBattlers.Make("pikachu", "static", null, 100, "voltswitch");
            var bench1 = TestBattlers.Make("bulbasaur", "overgrow");
            var bench2 = TestBattlers.Make("snorlax", "thickfat");
            var foe = TestBattlers.Make("blissey", "naturalcure");
            var b = TestBattlers.SetupBattle(user, foe);
            b.Sides[0].Team.Add(bench1);
            b.Sides[0].Team.Add(bench2);

            // Choose the SECOND bench mon (index 2) — auto-pick would take bench1 (index 1).
            b.Step(Choice.UseMove("voltswitch", pivotTo: 2), new Choice { Kind = ChoiceKind.Skip });
            Assert.Same(bench2, b.Sides[0].ActiveSlots[0]);
        }

        [Fact]
        public void Disguise_AbsorbsFirstHit()
        {
            var miku = TestBattlers.Make("mimikyu", "disguise");
            var atk = TestBattlers.Make("lucario", "innerfocus", null, 100, "closecombat");
            var b = TestBattlers.SetupBattle(atk, miku);
            int max = miku.MaxStats[(int)Stat.HP];
            b.Step(Choice.UseMove("closecombat"), Skip);
            Assert.False(miku.IsFainted);                 // the big hit was absorbed
            Assert.True(miku.CurrentHp >= max - max / 8 - 1); // only the 1/8 bust chip
        }
    }
}
