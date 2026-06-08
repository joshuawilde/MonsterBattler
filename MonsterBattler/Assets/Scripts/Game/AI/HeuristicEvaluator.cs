using System;
using System.Collections.Generic;
using MonsterBattler.Sim;
using MonsterBattler.Sim.Data;

namespace MonsterBattler.Game.AI
{
    /// <summary>A legal action paired with its heuristic value (higher = better).</summary>
    public struct ActionScore
    {
        public Choice Choice;
        public float Value;
    }

    /// <summary>
    /// Damage-aware heuristic scorer for one side's legal actions. Pure C# (no UnityEngine) so it
    /// also runs in the headless calibration tournament. It reads full battle state (a PvE bot may
    /// "see" the opponent); strength is dialed down separately by <see cref="EloBattleAI"/>.
    ///
    /// Scale (roughly comparable across action types):
    ///   • damaging move: fraction of the foe's CURRENT hp it deals × accuracy, +1.0 for a KO
    ///     (+0.6 more if we move first), so guaranteed KOs dominate chip;
    ///   • status/setup move: ~0.45 when useful, 0.15 otherwise;
    ///   • switch: 0..0.9 based on how much safer + how much it threatens back.
    /// </summary>
    public static class HeuristicEvaluator
    {
        public static List<ActionScore> Score(Battle battle, Side own, Side opp)
        {
            var list = new List<ActionScore>();
            var me = own.ActiveSlots.Count > 0 ? own.ActiveSlots[0] : null;
            var foe = opp.ActiveSlots.Count > 0 ? opp.ActiveSlots[0] : null;
            if (me == null || me.IsFainted) return list;

            if (!string.IsNullOrEmpty(me.LockedMoveId)) // choice-lock / two-turn: only one legal move
            {
                list.Add(new ActionScore { Choice = Choice.UseMove(me.LockedMoveId), Value = 1f });
                return list;
            }

            bool meFaster = foe != null && EffSpe(me) >= EffSpe(foe);

            for (int i = 0; i < me.Moves.Count; i++)
            {
                var slot = me.Moves[i];
                if (slot.Disabled) continue;
                list.Add(new ActionScore { Choice = Choice.UseMove(slot.Move.Id), Value = ScoreMove(battle, me, foe, slot.Move, meFaster) });
            }

            if (!battle.IsTrapped(me)) // a trapping ability removes switch options
                for (int i = 0; i < own.Team.Count; i++)
                {
                    var p = own.Team[i];
                    if (p == null || p == me || p.IsFainted) continue;
                    list.Add(new ActionScore { Choice = Choice.SwitchTo(i), Value = ScoreSwitch(me, p, foe) });
                }

            if (list.Count == 0 && me.Moves.Count > 0)
                list.Add(new ActionScore { Choice = Choice.UseMove(me.Moves[0].Move.Id), Value = 0f });
            return list;
        }

        static float ScoreMove(Battle battle, Pokemon me, Pokemon foe, MoveData mv, bool meFaster)
        {
            float acc = mv.Accuracy <= 0 ? 1f : mv.Accuracy / 100f;
            if (mv.Category == MoveCategory.Status || mv.BasePower <= 0)
                return ScoreStatusMove(me, foe, mv) * acc;
            if (foe == null) return 0.1f;

            int dmg = DamageCalc.Compute(battle, me, foe, mv);
            float frac = foe.CurrentHp > 0 ? Math.Min(1f, (float)dmg / foe.CurrentHp) : 1f;
            float v = frac * acc;
            if (dmg >= foe.CurrentHp) // KO
            {
                v += 1.0f;
                if (meFaster || mv.Priority > 0) v += 0.6f;
            }
            return v;
        }

        static float ScoreStatusMove(Pokemon me, Pokemon foe, MoveData mv)
        {
            if (mv.SelfBoosts != null && mv.SelfBoosts.Length > 0 &&
                me.CurrentHp > me.MaxStats[(int)Stat.HP] * 0.6f)
                return 0.45f; // set up while healthy
            return 0.3f;      // status / utility — modest, below a solid attack
        }

        // Switching is only worth it to ESCAPE a bad matchup — it must stay BELOW a decent attack so
        // the AI doesn't switch every turn. Near-zero unless our active is hit hard (>=2x) by the foe
        // AND the candidate is meaningfully safer.
        static float ScoreSwitch(Pokemon me, Pokemon cand, Pokemon foe)
        {
            if (foe == null) return 0.04f;
            float meThreat = WorstIncoming(foe, me);     // foe's best type multiplier vs our active
            float candThreat = WorstIncoming(foe, cand); // …vs the candidate
            if (meThreat < 2f || candThreat >= meThreat) return 0.04f; // not threatened / no improvement
            float escape = meThreat - candThreat;         // how much danger we dodge (e.g. 2 -> 0.5 = 1.5)
            return 0.30f + 0.20f * Math.Min(1f, escape / 2f); // ~0.30..0.50, always below a KO/strong hit
        }

        static float WorstIncoming(Pokemon attacker, Pokemon defender)
        {
            float worst = 0f;
            foreach (var t in TypesOf(attacker))
                if (t != MonType.None) worst = Math.Max(worst, Eff(t, defender));
            return worst <= 0f ? 1f : worst;
        }

        static float Eff(MonType t, Pokemon def)
        {
            var (d1, d2) = TypePair(def);
            return TypeChart.Effectiveness(t, d1, d2);
        }

        static (MonType, MonType) TypePair(Pokemon m) =>
            m.IsTerastallized ? (m.TeraType, MonType.None) : (m.Species.Type1, m.Species.Type2);

        static IEnumerable<MonType> TypesOf(Pokemon m)
        {
            var (a, b) = TypePair(m);
            yield return a;
            yield return b;
        }

        static float EffSpe(Pokemon m) => m.MaxStats[(int)Stat.Spe] * Stats.StageMult(m.StatStages[(int)Stat.Spe]);
    }
}
