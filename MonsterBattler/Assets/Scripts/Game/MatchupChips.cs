using System.Collections.Generic;
using MonsterBattler.Sim;
using UnityEngine;

namespace MonsterBattler.Game
{
    /// <summary>
    /// Pure matchup read-out for one of the player's mons vs the enemy's ACTIVE mon: SPEED / DAMAGE /
    /// HP expressed as "yours ÷ theirs" multipliers (e.g. "1.5× FAST", "10× DMG", "0.5× HP").
    /// Deterministic — never touches the battle PRNG, so it's safe to call every UI refresh.
    /// DAMAGE is a trade ratio: your best move's % of their HP vs their best move's % of yours.
    /// </summary>
    public static class MatchupChips
    {
        public struct Chip
        {
            public string label;
            public Color color;
            public Chip(string l, Color c) { label = l; color = c; }
        }

        static readonly Color Good = new Color(0.31f, 0.80f, 0.36f);
        static readonly Color Bad = new Color(0.88f, 0.30f, 0.27f);
        static readonly Color Even = new Color(0.78f, 0.80f, 0.85f);

        static Color Col(float r) => r >= 1.05f ? Good : r <= 0.95f ? Bad : Even;
        static float EffStat(Pokemon m, Stat s) => m.MaxStats[(int)s] * Stats.StageMult(m.StatStages[(int)s]);

        static float EffSpeed(Pokemon m)
        {
            float sp = EffStat(m, Stat.Spe);
            if (m.Status == StatusCondition.Paralysis) sp *= 0.5f;
            if (m.HasItem("choicescarf")) sp *= 1.5f;
            return Mathf.Max(1f, sp);
        }

        // Best damaging move's estimated fraction of the defender's max HP. Same formula both ways,
        // so the ratio is meaningful even though it omits level/items/abilities (relative, not exact).
        static float BestDamageFrac(Pokemon atkr, Pokemon defr)
        {
            var (dt1, dt2) = defr.CurrentTypes();
            var (at1, at2) = atkr.CurrentTypes();
            float best = 0f;
            foreach (var slot in atkr.Moves)
            {
                var mv = slot.Move;
                if (mv == null || mv.Category == MoveCategory.Status || mv.BasePower <= 0) continue;
                float stab = (mv.Type == at1 || mv.Type == at2) ? 1.5f : 1f;
                float eff = TypeChart.Effectiveness(mv.Type, dt1, dt2);
                bool phys = mv.Category == MoveCategory.Physical;
                float atk = EffStat(atkr, phys ? Stat.Atk : Stat.SpA);
                float def = Mathf.Max(1f, EffStat(defr, phys ? Stat.Def : Stat.SpD));
                best = Mathf.Max(best, mv.BasePower * stab * eff * atk / def);
            }
            return best / Mathf.Max(1f, defr.MaxStats[(int)Stat.HP]);
        }

        static string Fmt(float r) => r >= 10f ? $"{r:0}×" : $"{r:0.0}×";

        public static List<Chip> Build(Pokemon mine, Pokemon enemy)
        {
            var chips = new List<Chip>(3);
            if (mine == null || enemy == null || mine.IsFainted || enemy.IsFainted) return chips;

            float spd = EffSpeed(mine) / EffSpeed(enemy);
            chips.Add(spd >= 1f ? new Chip($"{Fmt(spd)}F", Col(spd))
                                : new Chip($"{Fmt(1f / spd)}S", Bad));

            float dmg = BestDamageFrac(mine, enemy) / Mathf.Max(0.0001f, BestDamageFrac(enemy, mine));
            chips.Add(new Chip($"{Fmt(dmg)}D", Col(dmg)));

            float hp = (float)mine.CurrentHp / Mathf.Max(1, enemy.CurrentHp);
            chips.Add(new Chip($"{Fmt(hp)}H", Col(hp)));
            return chips;
        }
    }
}
