using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    public sealed class SafeguardMoveEffect : Effect
    {
        public override string EffectId => "safeguardmove";
        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var side = ev.Battle.SideOf(ev.User);
            if (side == null) return;
            var c = ev.Battle.AddSideCondition(side, "safeguard", maxLayers: 1, turns: 5);
            if (c != null) c.TurnsLeft = 5;
        }
    }
}
