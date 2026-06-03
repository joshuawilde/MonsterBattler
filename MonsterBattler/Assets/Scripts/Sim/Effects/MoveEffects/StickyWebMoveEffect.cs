using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    public sealed class StickyWebMoveEffect : Effect
    {
        public override string EffectId => "stickywebmove";
        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var foeSide = ev.Battle.OpposingSideOf(ev.User);
            if (foeSide == null) return;
            ev.Battle.AddSideCondition(foeSide, "stickyweb", maxLayers: 1);
        }
    }
}
