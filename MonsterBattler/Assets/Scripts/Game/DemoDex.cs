using MonsterBattler.Sim;
using MonsterBattler.Sim.Data;

namespace MonsterBattler.Game
{
    /// <summary>
    /// Throwaway in-memory data for bringing the engine up before we have real JSON loading.
    /// Replace with a proper StreamingAssets JSON loader once the data shape is settled.
    /// </summary>
    public static class DemoDex
    {
        public static Dex Build()
        {
            var dex = new Dex();

            dex.Species["bulbasaur"] = new SpeciesData
            {
                Id = "bulbasaur", Name = "Bulbasaur",
                Type1 = MonType.Grass, Type2 = MonType.Poison,
                BaseStats = new BaseStats { HP = 45, Atk = 49, Def = 49, SpA = 65, SpD = 65, Spe = 45 },
            };
            dex.Species["charmander"] = new SpeciesData
            {
                Id = "charmander", Name = "Charmander",
                Type1 = MonType.Fire,
                BaseStats = new BaseStats { HP = 39, Atk = 52, Def = 43, SpA = 60, SpD = 50, Spe = 65 },
            };

            // Abilities — EffectId left null falls back to Id, matching EffectRegistry by class.
            dex.Abilities["blaze"]     = new AbilityData { Id = "blaze",     Name = "Blaze" };
            dex.Abilities["levitate"]  = new AbilityData { Id = "levitate",  Name = "Levitate" };

            // Moves
            dex.Moves["tackle"] = new MoveData
            {
                Id = "tackle", Name = "Tackle",
                Type = MonType.Normal, Category = MoveCategory.Physical,
                BasePower = 40, Accuracy = 100, Pp = 35, Contact = true,
            };
            dex.Moves["ember"] = new MoveData
            {
                Id = "ember", Name = "Ember",
                Type = MonType.Fire, Category = MoveCategory.Special,
                BasePower = 40, Accuracy = 100, Pp = 25,
                EffectId = "ember",
            };
            dex.Moves["vinewhip"] = new MoveData
            {
                Id = "vinewhip", Name = "Vine Whip",
                Type = MonType.Grass, Category = MoveCategory.Physical,
                BasePower = 45, Accuracy = 100, Pp = 25, Contact = true,
            };
            return dex;
        }

        public static Pokemon MakeBattler(Dex dex, string speciesId, string nickname,
                                          string abilityId = null, string[] moveIds = null, string itemId = null,
                                          MonType teraType = MonType.None)
        {
            var sp = dex.Get(speciesId);
            var mon = new Pokemon { Species = sp, Nickname = nickname, Level = 50 };
            if (!string.IsNullOrEmpty(abilityId)) mon.Ability = dex.GetAbility(abilityId);
            if (!string.IsNullOrEmpty(itemId)) mon.Item = dex.GetItem(itemId);
            // Default Tera Type to the species' primary type if not specified.
            mon.TeraType = teraType != MonType.None ? teraType : sp.Type1;

            for (int i = 0; i < 6; i++) mon.IVs[i] = 31;
            mon.MaxStats[(int)Stat.HP]  = CalcHp(sp.BaseStats.HP, mon.Level);
            mon.MaxStats[(int)Stat.Atk] = CalcStat(sp.BaseStats.Atk, mon.Level);
            mon.MaxStats[(int)Stat.Def] = CalcStat(sp.BaseStats.Def, mon.Level);
            mon.MaxStats[(int)Stat.SpA] = CalcStat(sp.BaseStats.SpA, mon.Level);
            mon.MaxStats[(int)Stat.SpD] = CalcStat(sp.BaseStats.SpD, mon.Level);
            mon.MaxStats[(int)Stat.Spe] = CalcStat(sp.BaseStats.Spe, mon.Level);
            mon.CurrentHp = mon.MaxStats[(int)Stat.HP];

            moveIds ??= new[] { "tackle" };
            foreach (var id in moveIds)
            {
                var move = dex.GetMove(id);
                mon.Moves.Add(new MoveSlot { Move = move, Pp = move.Pp, MaxPp = move.Pp });
            }
            return mon;
        }

        static int CalcHp(int baseStat, int level)
            => (2 * baseStat + 31) * level / 100 + level + 10;
        static int CalcStat(int baseStat, int level)
            => (2 * baseStat + 31) * level / 100 + 5;
    }
}
