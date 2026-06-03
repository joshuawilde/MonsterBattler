using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.SideConditions
{
    /// <summary>
    /// Wish: 2 turns after use, heals the active slot for 1/2 of the wisher's max HP.
    /// <see cref="SideCondition.Data"/> stores the heal amount (snapshot of wisher.MaxHp/2).
    /// </summary>
    public sealed class WishCondition : Effect
    {
        public override string EffectId => "wish";
        public override string DisplayName => "Wish";

        public override void OnResidual(ResidualEvent ev, Pokemon owner)
        {
            var side = ev.Battle.SideOf(owner);
            if (side == null) return;
            if (!side.Conditions.TryGetValue("wish", out var cond)) return;
            // We rely on the side-condition tick (TickSideConditions) to count down TurnsLeft.
            // When it hits 1, this residual fires the heal, then the tick removes the condition.
            if (cond.TurnsLeft != 1) return;
            int heal = cond.Data is int snapshot ? snapshot : owner.MaxStats[(int)Stat.HP] / 2;
            int actual = System.Math.Min(heal, owner.MaxStats[(int)Stat.HP] - owner.CurrentHp);
            if (actual <= 0) return;
            owner.CurrentHp += actual;
            ev.Battle.Log.Raw($"|-heal|{owner.Species?.Name ?? owner.Nickname}|{owner.CurrentHp}/{owner.MaxStats[(int)Stat.HP]}|[from] move: Wish");
        }
    }
}
