using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Rapid Spin: clears hazards from the user's side and raises user Speed by 1 (gen 8+).</summary>
    public sealed class RapidSpinMoveEffect : Effect
    {
        public override string EffectId => "rapidspinmove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var side = ev.Battle.SideOf(ev.User);
            if (side != null)
            {
                ev.Battle.RemoveSideCondition(side, "stealthrock");
                ev.Battle.RemoveSideCondition(side, "spikes");
                ev.Battle.RemoveSideCondition(side, "toxicspikes");
                ev.Battle.RemoveSideCondition(side, "stickyweb");
            }
        }

        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            ev.Battle.BoostStat(ev.User, Stat.Spe, +1);
        }
    }
}
