using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.SideConditions
{
    /// <summary>
    /// Stealth Rock: incoming Pokemon take damage scaled by their Rock-type weakness.
    /// Damage = maxHp × RockTypeEffectiveness / 8 (so 1/8 neutral, 1/16 resist, 1/4 weak,
    /// 1/32 4× resist, 1/2 4× weak, 0 immune).
    /// </summary>
    public sealed class StealthRockCondition : Effect
    {
        public override string EffectId => "stealthrock";
        public override string DisplayName => "Stealth Rock";

        public override void OnSwitchIn(SwitchInEvent ev, Pokemon owner)
        {
            // owner here is the switching-in Pokemon (passed from RunSwitchIn).
            var mon = owner;
            if (mon == null || mon.IsFainted || mon.Species == null) return;
            float eff = TypeChart.Effectiveness(MonType.Rock, mon.Species.Type1, mon.Species.Type2);
            if (eff <= 0f) return;
            int dmg = (int)(mon.MaxStats[(int)Stat.HP] * eff / 8f);
            if (dmg <= 0) return;
            ev.Battle.ApplyDamage(mon, dmg, DamageSource.Hazard);
            ev.Battle.Log.Raw($"|-damage|{mon.Species?.Name ?? mon.Nickname}|{mon.CurrentHp}/{mon.MaxStats[(int)Stat.HP]}|[from] Stealth Rock");
        }
    }
}
