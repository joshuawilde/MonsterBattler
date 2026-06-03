using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    public sealed class ReflectMoveEffect : Effect
    {
        public override string EffectId => "reflectmove";
        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var side = ev.Battle.SideOf(ev.User);
            if (side == null) return;
            var cond = ev.Battle.AddSideCondition(side, "reflect", maxLayers: 1, turns: 5);
            if (cond != null) cond.TurnsLeft = 5;
        }
    }
}
