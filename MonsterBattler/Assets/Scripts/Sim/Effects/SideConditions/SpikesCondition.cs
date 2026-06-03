using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.SideConditions
{
    /// <summary>Spikes: 1/8 → 1/6 → 1/4 max HP on switch-in by layer count. Flying/Levitate immune.</summary>
    public sealed class SpikesCondition : Effect
    {
        public override string EffectId => "spikes";
        public override string DisplayName => "Spikes";

        public override void OnSwitchIn(SwitchInEvent ev, Pokemon owner)
        {
            var mon = owner;
            if (mon == null || mon.IsFainted || mon.Species == null) return;
            if (IsType(mon, MonType.Flying)) return;
            if (mon.AbilityEffect is Abilities.LevitateEffect) return;

            var side = ev.Battle.SideOf(mon);
            if (side == null || !side.Conditions.TryGetValue("spikes", out var cond)) return;
            int layers = System.Math.Clamp(cond.Layers, 1, 3);
            int denom = layers switch { 1 => 8, 2 => 6, 3 => 4, _ => 8 };
            int dmg = System.Math.Max(1, mon.MaxStats[(int)Stat.HP] / denom);
            ev.Battle.ApplyDamage(mon, dmg, DamageSource.Hazard);
            ev.Battle.Log.Raw($"|-damage|{mon.Species?.Name ?? mon.Nickname}|{mon.CurrentHp}/{mon.MaxStats[(int)Stat.HP]}|[from] Spikes");
        }

        static bool IsType(Pokemon m, MonType t) => m?.Species != null && (m.Species.Type1 == t || m.Species.Type2 == t);
    }
}
