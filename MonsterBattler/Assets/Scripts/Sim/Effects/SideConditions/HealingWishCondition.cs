using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.SideConditions
{
    /// <summary>
    /// Healing Wish: persists until the next mon switches in on this side. On switch-in,
    /// fully restores their HP and clears any non-volatile status, then consumes itself.
    /// </summary>
    public sealed class HealingWishCondition : Effect
    {
        public override string EffectId => "healingwish";
        public override string DisplayName => "Healing Wish";

        public override void OnSwitchIn(SwitchInEvent ev, Pokemon owner)
        {
            var mon = owner;
            if (mon == null || mon.IsFainted) return;
            var side = ev.Battle.SideOf(mon);
            if (side == null) return;
            int max = mon.MaxStats[(int)Stat.HP];
            mon.CurrentHp = max;
            mon.Status = StatusCondition.None;
            mon.StatusEffect = null;
            mon.ToxicCounter = 0;
            mon.SleepTurnsLeft = 0;
            ev.Battle.Log.Raw($"|-heal|{mon.Species?.Name ?? mon.Nickname}|{max}/{max}|[from] move: Healing Wish");
            ev.Battle.RemoveSideCondition(side, "healingwish");
        }
    }
}
