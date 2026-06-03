using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Levitate: owner is immune to Ground-type moves.
    /// </summary>
    public sealed class LevitateEffect : Effect
    {
        public override string EffectId => "levitate";
        public override string DisplayName => "Levitate";

        public override void OnTryHit(TryHitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            if (ev.Move?.Type == MonType.Ground)
            {
                ev.Blocked = true;
                ev.BlockReason = "Levitate";
            }
        }
    }
}
