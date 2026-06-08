using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Heavy Slam / Heat Crash: base power scales with how much heavier the user is than the target.</summary>
    public abstract class WeightRatioBase : Effect
    {
        public override void OnBasePower(BasePowerEvent ev, Pokemon owner)
        {
            if (ev.User == null || ev.Target == null) return;
            float ratio = ev.Battle.EffectiveWeight(ev.User) / ev.Battle.EffectiveWeight(ev.Target);
            ev.BasePower = ratio >= 5f ? 120 : ratio >= 4f ? 100 : ratio >= 3f ? 80 : ratio >= 2f ? 60 : 40;
        }
    }
    public sealed class HeavySlamMoveEffect : WeightRatioBase { public override string EffectId => "heavyslammove"; }
    public sealed class HeatCrashMoveEffect : WeightRatioBase { public override string EffectId => "heatcrashmove"; }
}
