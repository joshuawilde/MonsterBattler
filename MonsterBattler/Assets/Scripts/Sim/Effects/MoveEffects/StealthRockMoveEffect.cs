using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    public sealed class StealthRockMoveEffect : Effect
    {
        public override string EffectId => "stealthrockmove";
        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var foeSide = ev.Battle.OpposingSideOf(ev.User);
            if (foeSide == null) return;
            ev.Battle.AddSideCondition(foeSide, "stealthrock", maxLayers: 1);
        }
    }
}
