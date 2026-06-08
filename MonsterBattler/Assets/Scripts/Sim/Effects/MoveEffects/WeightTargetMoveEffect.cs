using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Low Kick / Grass Knot: base power scales with the target's weight.</summary>
    public abstract class WeightTargetBase : Effect
    {
        public override void OnBasePower(BasePowerEvent ev, Pokemon owner)
        {
            if (ev.Target == null) return;
            float w = ev.Battle.EffectiveWeight(ev.Target);
            ev.BasePower = w >= 200f ? 120 : w >= 100f ? 100 : w >= 50f ? 80 : w >= 25f ? 60 : w >= 10f ? 40 : 20;
        }
    }
    public sealed class LowKickMoveEffect : WeightTargetBase { public override string EffectId => "lowkickmove"; }
    public sealed class GrassKnotMoveEffect : WeightTargetBase { public override string EffectId => "grassknotmove"; }
}
