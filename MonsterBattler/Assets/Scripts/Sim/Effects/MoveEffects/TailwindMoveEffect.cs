using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    public sealed class TailwindMoveEffect : Effect
    {
        public override string EffectId => "tailwindmove";
        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var side = ev.Battle.SideOf(ev.User);
            if (side == null) return;
            var cond = ev.Battle.AddSideCondition(side, "tailwind", maxLayers: 1, turns: 4);
            if (cond != null) cond.TurnsLeft = 4;
        }
    }
}
