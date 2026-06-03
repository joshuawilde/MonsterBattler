namespace MonsterBattler.Sim
{
    /// <summary>
    /// Stat-stage multiplier tables for gen 9.
    ///
    /// Atk / Def / SpA / SpD / Spe use the (2+stage)/2 ratio formula (clamped at ±6).
    /// Accuracy and Evasion use a different 3/3 table.
    /// </summary>
    public static class Stats
    {
        /// <summary>Multiplier for Atk/Def/SpA/SpD/Spe stat stages (-6..+6).</summary>
        public static float StageMult(int stage)
        {
            stage = System.Math.Clamp(stage, -6, 6);
            return stage >= 0 ? (2f + stage) / 2f : 2f / (2f - stage);
        }

        /// <summary>Multiplier for Accuracy/Evasion stages (-6..+6) — different table from stat stages.</summary>
        public static float AccuracyStageMult(int stage)
        {
            stage = System.Math.Clamp(stage, -6, 6);
            return stage >= 0 ? (3f + stage) / 3f : 3f / (3f - stage);
        }
    }
}
