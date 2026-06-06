using MonsterBattler.Sim;
using MonsterBattler.Sim.Data;

namespace MonsterBattler.Sim.Tests
{
    /// <summary>Builds battlers and battles for tests, using the real Dex.</summary>
    public static class TestBattlers
    {
        /// <summary>
        /// Make a battler with 0 EVs / 31 IVs at the given level (clean numbers; base-stat ordering
        /// is preserved, which is what the paradox-boost "highest stat" logic keys off of).
        /// AbilityEffect/ItemEffect are resolved later by Battle.Setup, matching real usage.
        /// </summary>
        public static Pokemon Make(string speciesId, string ability = null, string item = null,
                                   int level = 100, params string[] moves)
        {
            var dex = TestData.Dex;
            var sp = dex.Get(speciesId);
            var mon = new Pokemon { Species = sp, Nickname = sp.Name, Level = level, TeraType = sp.Type1 };
            if (ability != null) mon.Ability = dex.GetAbility(ability);
            if (item != null) mon.Item = dex.GetItem(item);
            for (int i = 0; i < 6; i++) { mon.IVs[i] = 31; mon.EVs[i] = 0; }

            var b = sp.BaseStats;
            mon.MaxStats[(int)Stat.HP]  = (2 * b.HP  + 31) * level / 100 + level + 10;
            mon.MaxStats[(int)Stat.Atk] = (2 * b.Atk + 31) * level / 100 + 5;
            mon.MaxStats[(int)Stat.Def] = (2 * b.Def + 31) * level / 100 + 5;
            mon.MaxStats[(int)Stat.SpA] = (2 * b.SpA + 31) * level / 100 + 5;
            mon.MaxStats[(int)Stat.SpD] = (2 * b.SpD + 31) * level / 100 + 5;
            mon.MaxStats[(int)Stat.Spe] = (2 * b.Spe + 31) * level / 100 + 5;
            mon.CurrentHp = mon.MaxStats[(int)Stat.HP];

            foreach (var id in moves)
            {
                var move = dex.GetMove(id);
                mon.Moves.Add(new MoveSlot { Move = move, Pp = move.Pp, MaxPp = move.Pp });
            }
            return mon;
        }

        /// <summary>Set up a singles battle with one lead per side. Runs Battle.Setup (which resolves
        /// ability/item effects and fires switch-in for both leads).</summary>
        public static Battle SetupBattle(Pokemon player, Pokemon opponent, ulong seed = 1)
        {
            var battle = new Battle(TestData.Dex, seed);
            var s0 = new Side { Name = "Player" };
            s0.Team.Add(player); s0.ActiveSlots.Add(player); player.IsActive = true;
            var s1 = new Side { Name = "Opponent" };
            s1.Team.Add(opponent); s1.ActiveSlots.Add(opponent); opponent.IsActive = true;
            battle.Setup(s0, s1);
            return battle;
        }
    }
}
