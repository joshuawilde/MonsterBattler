using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Transform: the user becomes a copy of the target.</summary>
    public sealed class TransformMoveEffect : Effect
    {
        public override string EffectId => "transformmove";
        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            if (ev.User != null && ev.Target != null && !ev.User.Tags.Contains("transformed"))
                ev.Battle.Transform(ev.User, ev.Target);
        }
    }
}
