using System.Collections.Generic;
using System.Linq;
using MonsterBattler.Sim.Data;

namespace MonsterBattler.Sim
{
    /// <summary>
    /// Generates Pokemon Showdown gen9-flavoured random-battle teams from the curated
    /// <see cref="RandbatsDex"/> sets data.
    ///
    /// FIDELITY: this is a faithful port of the parts of PS's <c>randomSet</c> that carry the most
    /// weight — it draws from the same curated movepools, uses the same per-species levels, the same
    /// flat 85-EV spread with the documented atk/spe trims, and samples ability/tera from the set.
    /// Move selection approximates PS's <c>randomMoveset</c>: it honours role-enforced moves and
    /// guarantees a STAB + tera-STAB move, then fills the rest from the pool. It does NOT yet
    /// reproduce PS's full <c>queryMoves</c> counter system, coverage/redundancy culling, the
    /// weakness-balanced team constraints, or the ~200-line <c>getItem</c> heuristic (item uses a
    /// compact role-based stand-in). Teams are Showdown-like, not byte-identical. See README in
    /// tools/dex-import for what remains.
    /// </summary>
    public sealed class RandomTeamGenerator
    {
        const int MaxMoves = 4;

        readonly Dex _dex;
        readonly RandbatsDex _rb;
        readonly Prng _prng;

        // Moves that scale off a stat other than the user's Atk, or deal fixed damage — their
        // presence does NOT justify keeping Atk EVs (mirrors PS's noAttackStatMoves check).
        static readonly HashSet<string> AtkIrrelevantPhysical = new()
        {
            "bodypress", "foulplay", "seismictoss", "nightshade", "endeavor", "superfang",
            "dragonrage", "sonicboom", "finalgambit", "metalburst", "counter",
        };

        static readonly HashSet<string> SetupRoles = new()
        {
            "Setup Sweeper", "Bulky Setup", "Fast Bulky Setup",
        };

        public RandomTeamGenerator(Dex dex, RandbatsDex randbats, Prng prng)
        {
            _dex = dex;
            _rb = randbats;
            _prng = prng;
        }

        public List<Pokemon> GenerateTeam(int size = 6)
        {
            var team = new List<Pokemon>();
            foreach (var id in PickSpecies(size))
                team.Add(BuildSet(id));
            return team;
        }

        // --- species selection ----------------------------------------------------------------
        // Simplified vs PS: distinct random species, no base-species de-dup or weakness balancing.
        List<string> PickSpecies(int size)
        {
            var pool = _rb.Species.Keys.Where(id => _dex.Species.ContainsKey(id)).ToList();
            var chosen = new List<string>();
            while (chosen.Count < size && pool.Count > 0)
            {
                int i = _prng.Range(0, pool.Count);
                chosen.Add(pool[i]);
                pool.RemoveAt(i); // distinct
            }
            return chosen;
        }

        // --- single set -----------------------------------------------------------------------
        Pokemon BuildSet(string speciesId)
        {
            var sp = _dex.Get(speciesId);
            var entry = _rb.Species[speciesId];
            var set = Sample(entry.Sets);

            var teraType = set.TeraTypes.Count > 0 ? Sample(set.TeraTypes) : sp.Type1;
            var moves = BuildMoves(sp, set, teraType);
            var abilityId = set.AbilityIds.Count > 0 ? Sample(set.AbilityIds)
                          : (sp.AbilityIds.Count > 0 ? sp.AbilityIds[0] : null);
            var itemId = PickItem(set, abilityId, moves);
            int level = entry.Level > 0 ? entry.Level : 80;

            var mon = new Pokemon
            {
                Species = sp,
                Nickname = sp.Name,
                Level = level,
                TeraType = teraType,
                Gender = Gender.Genderless,
            };
            if (!string.IsNullOrEmpty(abilityId) && _dex.Abilities.ContainsKey(abilityId))
                mon.Ability = _dex.GetAbility(abilityId);
            if (!string.IsNullOrEmpty(itemId) && _dex.Items.ContainsKey(itemId))
                mon.Item = _dex.GetItem(itemId);

            AssignStats(mon, moves);

            foreach (var id in moves)
            {
                var move = _dex.GetMove(id);
                mon.Moves.Add(new MoveSlot { Move = move, Pp = move.Pp, MaxPp = move.Pp });
            }
            mon.CurrentHp = mon.MaxStats[(int)Stat.HP];
            return mon;
        }

        // --- move selection (approx PS randomMoveset) -----------------------------------------
        List<string> BuildMoves(SpeciesData sp, RandbatsSet set, MonType teraType)
        {
            var pool = new List<string>(set.MovepoolIds);
            var chosen = new List<string>();

            void Take(string id)
            {
                if (id != null && pool.Remove(id) && !chosen.Contains(id) && chosen.Count < MaxMoves)
                    chosen.Add(id);
            }

            // Whole pool fits — just take it (matches PS shortcut).
            if (pool.Count <= MaxMoves)
                return new List<string>(set.MovepoolIds);

            // Role-enforced moves we can see directly in PS's randomMoveset.
            if (set.Role == "Tera Blast user") Take("terablast");
            if (set.Role == "Bulky Support")
            {
                Take("rapidspin");
                Take("defog");
            }
            if (pool.Contains("stickyweb")) Take("stickyweb");

            var types = new HashSet<MonType> { sp.Type1 };
            if (sp.Type2 != MonType.None) types.Add(sp.Type2);

            // Guarantee a STAB move (a damaging move sharing one of the species' types).
            if (!chosen.Any(id => IsStab(id, types)))
                Take(SampleOrNull(pool.Where(id => IsStab(id, types)).ToList()));

            // Guarantee a tera-STAB damaging move (PS biases toward this for non-support roles).
            if (set.Role != "Bulky Support" && !chosen.Any(id => IsTypeDamage(id, teraType)))
                Take(SampleOrNull(pool.Where(id => IsTypeDamage(id, teraType)).ToList()));

            // Fill remaining slots from the pool.
            while (chosen.Count < MaxMoves && pool.Count > 0)
                Take(pool[_prng.Range(0, pool.Count)]);

            Shuffle(chosen);
            return chosen;
        }

        bool IsStab(string moveId, HashSet<MonType> types)
        {
            var m = _dex.GetMove(moveId);
            return m.BasePower > 0 && types.Contains(m.Type);
        }

        bool IsTypeDamage(string moveId, MonType type)
        {
            var m = _dex.GetMove(moveId);
            return m.BasePower > 0 && m.Type == type;
        }

        // --- item (compact stand-in for PS getItem) -------------------------------------------
        string PickItem(RandbatsSet set, string abilityId, List<string> moves)
        {
            if (abilityId == "protosynthesis" || abilityId == "quarkdrive") return "boosterenergy";
            if (set.Role == "AV Pivot") return "assaultvest";
            if (set.Role == "Fast Support") return "heavydutyboots";

            bool hasSetup = SetupRoles.Contains(set.Role);
            if (set.Role == "Fast Attacker" || set.Role == "Wallbreaker")
            {
                var dmg = moves.Select(_dex.GetMove).Where(m => m.BasePower > 0).ToList();
                if (dmg.Count > 0 && dmg.All(m => m.Category == MoveCategory.Physical)) return "choiceband";
                if (dmg.Count > 0 && dmg.All(m => m.Category == MoveCategory.Special)) return "choicespecs";
                return "lifeorb";
            }
            if (hasSetup) return "lifeorb";
            return "leftovers"; // Bulky Support / Bulky Attacker / fallback
        }

        // --- stats (PS formula, neutral nature, EV-aware) -------------------------------------
        void AssignStats(Pokemon mon, List<string> moves)
        {
            for (int i = 0; i < 6; i++) { mon.IVs[i] = 31; mon.EVs[i] = 85; }

            // atk EV trim: drop Atk if no physical move actually scales off the user's Atk.
            bool keepAtk = moves.Any(id =>
            {
                var m = _dex.GetMove(id);
                return m.Category == MoveCategory.Physical && m.BasePower > 0
                       && !AtkIrrelevantPhysical.Contains(id);
            });
            if (!keepAtk) { mon.EVs[(int)Stat.Atk] = 0; mon.IVs[(int)Stat.Atk] = 0; }

            // spe EV trim: Gyro Ball / Trick Room want minimum speed.
            if (moves.Contains("gyroball") || moves.Contains("trickroom"))
            {
                mon.EVs[(int)Stat.Spe] = 0; mon.IVs[(int)Stat.Spe] = 0;
            }

            var bs = mon.Species.BaseStats;
            int L = mon.Level;
            mon.MaxStats[(int)Stat.HP]  = Hp(bs.HP, mon.IVs[0], mon.EVs[0], L);
            mon.MaxStats[(int)Stat.Atk] = Other(bs.Atk, mon.IVs[1], mon.EVs[1], L);
            mon.MaxStats[(int)Stat.Def] = Other(bs.Def, mon.IVs[2], mon.EVs[2], L);
            mon.MaxStats[(int)Stat.SpA] = Other(bs.SpA, mon.IVs[3], mon.EVs[3], L);
            mon.MaxStats[(int)Stat.SpD] = Other(bs.SpD, mon.IVs[4], mon.EVs[4], L);
            mon.MaxStats[(int)Stat.Spe] = Other(bs.Spe, mon.IVs[5], mon.EVs[5], L);
        }

        static int Hp(int b, int iv, int ev, int level)
            => (2 * b + iv + ev / 4) * level / 100 + level + 10;
        static int Other(int b, int iv, int ev, int level)
            => (2 * b + iv + ev / 4) * level / 100 + 5;

        // --- prng helpers ---------------------------------------------------------------------
        T Sample<T>(IList<T> list) => list[_prng.Range(0, list.Count)];
        string SampleOrNull(IList<string> list) => list.Count == 0 ? null : list[_prng.Range(0, list.Count)];

        void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _prng.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
