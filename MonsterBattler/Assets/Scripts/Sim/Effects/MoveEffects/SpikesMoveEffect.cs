using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    public sealed class SpikesMoveEffect : Effect
    {
        public override string EffectId => "spikesmove";
        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var foeSide = ev.Battle.OpposingSideOf(ev.User);
            if (foeSide == null) return;
            ev.Battle.AddSideCondition(foeSide, "spikes", maxLayers: 3);
        }
    }
}
