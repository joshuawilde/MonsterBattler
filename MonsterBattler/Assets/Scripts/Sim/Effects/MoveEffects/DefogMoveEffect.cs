using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>
    /// Defog: lowers target's Evasion by 1, clears hazards and screens from BOTH sides
    /// (including the user's own — that's part of why Rapid Spin is preferred for hazard control).
    /// </summary>
    public sealed class DefogMoveEffect : Effect
    {
        public override string EffectId => "defogmove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            ev.Battle.BoostStat(ev.Target, Stat.Eva, -1);
            foreach (var side in ev.Battle.Sides)
            {
                if (side == null) continue;
                ev.Battle.RemoveSideCondition(side, "stealthrock");
                ev.Battle.RemoveSideCondition(side, "spikes");
                ev.Battle.RemoveSideCondition(side, "toxicspikes");
                ev.Battle.RemoveSideCondition(side, "stickyweb");
                ev.Battle.RemoveSideCondition(side, "reflect");
                ev.Battle.RemoveSideCondition(side, "lightscreen");
                ev.Battle.RemoveSideCondition(side, "auroraveil");
            }
        }
    }
}
