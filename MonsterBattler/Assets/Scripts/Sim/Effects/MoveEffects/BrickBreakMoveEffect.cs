using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Brick Break / Psychic Fangs: clears Reflect / Light Screen / Aurora Veil from the target's side before damage.</summary>
    public sealed class BrickBreakMoveEffect : Effect
    {
        public override string EffectId => "brickbreakmove";

        public override void OnTryHit(TryHitEvent ev, Pokemon owner)
        {
            var side = ev.Battle.SideOf(ev.Target);
            if (side == null) return;
            ev.Battle.RemoveSideCondition(side, "reflect");
            ev.Battle.RemoveSideCondition(side, "lightscreen");
            ev.Battle.RemoveSideCondition(side, "auroraveil");
        }
    }
}
