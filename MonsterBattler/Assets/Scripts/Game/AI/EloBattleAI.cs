using System;
using System.Collections.Generic;
using MonsterBattler.Sim;

namespace MonsterBattler.Game.AI
{
    /// <summary>
    /// Strength-tunable AI. It scores actions with <see cref="HeuristicEvaluator"/> (the ceiling) and
    /// then SELECTS via softmax over those values with a temperature T: T→0 plays optimally, larger T
    /// mixes in weaker actions. <see cref="SetElo"/> maps a target Elo → T via a calibration curve
    /// (provisional built-in curve until tools/calibrate-ai fills in measured anchors).
    /// </summary>
    public sealed class EloBattleAI : IBattleAI
    {
        // Calibration anchors (elo, temperature), ascending by elo. MEASURED by the UNIFIED ladder
        // (tools/calibrate-ai `unified`, 2026-06-06) — one Elo scale shared with SearchAI. This curve
        // covers the heuristic band (~871 random → ~1262 argmax); search rungs continue above it (see
        // BattleAIFactory). SetElo clamps outside this range.
        static List<(int elo, float t)> _curve = new List<(int, float)>
        {
            (851, 1.6f), (1006, 0.9f), (1080, 0.5f), (1137, 0.28f), (1229, 0.14f), (1271, 0.0f),
        };

        readonly Prng _prng;
        float _temperature;
        public int Elo { get; private set; }

        public EloBattleAI(int elo, ulong? seed = null)
        {
            _prng = seed.HasValue ? new Prng(seed.Value) : null;
            SetElo(elo);
        }

        public void SetElo(int elo)
        {
            Elo = elo;
            _temperature = EloToTemperature(elo);
        }

        /// <summary>Set the softmax temperature directly (used by the calibration tournament).</summary>
        public void SetTemperature(float t) => _temperature = t;

        /// <summary>Replace the Elo→temperature anchors (e.g. loaded from calibration output).</summary>
        public static void LoadCalibration(List<(int elo, float t)> curve)
        {
            if (curve != null && curve.Count >= 2)
            {
                curve.Sort((a, b) => a.elo.CompareTo(b.elo));
                _curve = curve;
            }
        }

        public Choice ChooseAction(Battle battle, Side ownSide, Side opponentSide)
        {
            var scored = HeuristicEvaluator.Score(battle, ownSide, opponentSide);
            if (scored.Count == 0) return Choice.UseMove("tackle");
            var rng = _prng ?? battle.Prng;
            return Sample(scored, _temperature, rng).Choice;
        }

        static ActionScore Sample(List<ActionScore> actions, float t, Prng rng)
        {
            if (t <= 0.001f) // argmax
            {
                var best = actions[0];
                for (int i = 1; i < actions.Count; i++) if (actions[i].Value > best.Value) best = actions[i];
                return best;
            }
            float max = float.NegativeInfinity;
            for (int i = 0; i < actions.Count; i++) if (actions[i].Value > max) max = actions[i].Value;
            var w = new double[actions.Count];
            double sum = 0;
            for (int i = 0; i < actions.Count; i++) { w[i] = Math.Exp((actions[i].Value - max) / t); sum += w[i]; }
            double r = rng.NextFloat() * sum, c = 0;
            for (int i = 0; i < actions.Count; i++) { c += w[i]; if (r <= c) return actions[i]; }
            return actions[actions.Count - 1];
        }

        /// <summary>Piecewise-linear interpolation over the calibration anchors.</summary>
        public static float EloToTemperature(int elo)
        {
            if (elo <= _curve[0].elo) return _curve[0].t;
            if (elo >= _curve[_curve.Count - 1].elo) return _curve[_curve.Count - 1].t;
            for (int i = 1; i < _curve.Count; i++)
            {
                if (elo <= _curve[i].elo)
                {
                    var (e0, t0) = _curve[i - 1];
                    var (e1, t1) = _curve[i];
                    float f = (float)(elo - e0) / (e1 - e0);
                    return t0 + (t1 - t0) * f;
                }
            }
            return _curve[_curve.Count - 1].t;
        }
    }
}
