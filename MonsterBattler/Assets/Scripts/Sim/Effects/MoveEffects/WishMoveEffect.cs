using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>
    /// Wish: queues a heal for 2 turns later. The Side condition's Data field stores the
    /// snapshot of the wisher's max HP / 2, so the heal is fixed at the wisher's potency
    /// even if a different mon is in when it resolves.
    /// </summary>
    public sealed class WishMoveEffect : Effect
    {
        public override string EffectId => "wishmove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var side = ev.Battle.SideOf(ev.User);
            if (side == null) return;
            // 2-turn delay: TurnsLeft 2 → ticks to 1 after this turn → WishCondition.OnResidual
            // fires the heal the following turn → ticks to 0 and the condition is removed.
            var c = ev.Battle.AddSideCondition(side, "wish", maxLayers: 1, turns: 2);
            if (c != null)
            {
                c.TurnsLeft = 2;
                c.Data = ev.User.MaxStats[(int)Stat.HP] / 2;
            }
        }
    }
}
