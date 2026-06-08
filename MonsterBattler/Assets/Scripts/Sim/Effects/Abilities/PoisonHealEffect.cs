namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Poison Heal: a poisoned holder heals 1/8 max HP each turn instead of taking poison damage.</summary>
    public sealed class PoisonHealEffect : Effect
    {
        public override string EffectId => "poisonheal";
        public override string DisplayName => "Poison Heal";

        /// <summary>If the mon has Poison Heal, heal 1/8 and return true (caller should skip the poison damage).</summary>
        public static bool HealInstead(Pokemon mon, Battle battle)
        {
            if (mon == null || !(mon.AbilityEffect is PoisonHealEffect)) return false;
            int max = mon.MaxStats[(int)Stat.HP];
            int heal = System.Math.Min(max / 8, max - mon.CurrentHp);
            if (heal > 0)
            {
                mon.CurrentHp += heal;
                battle.Log.Raw($"|-heal|{mon.Species?.Name ?? mon.Nickname}|{mon.CurrentHp}/{max}|[from] ability: Poison Heal");
            }
            return true;
        }
    }
}
