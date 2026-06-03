using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Pursuit: ×2 base power when the user is catching the target mid-switch (set by InterceptPursuit).</summary>
    public sealed class PursuitMoveEffect : Effect
    {
        public override string EffectId => "pursuit";

        public override void OnBasePower(BasePowerEvent ev, Pokemon owner)
        {
            if (ev.User != null && ev.User.Tags.Contains("pursuitintercept"))
                ev.BasePower *= 2;
        }
    }
}
