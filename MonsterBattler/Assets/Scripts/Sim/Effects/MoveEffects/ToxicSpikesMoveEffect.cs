using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    public sealed class ToxicSpikesMoveEffect : Effect
    {
        public override string EffectId => "toxicspikesmove";
        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var foeSide = ev.Battle.OpposingSideOf(ev.User);
            if (foeSide == null) return;
            ev.Battle.AddSideCondition(foeSide, "toxicspikes", maxLayers: 2);
        }
    }
}
