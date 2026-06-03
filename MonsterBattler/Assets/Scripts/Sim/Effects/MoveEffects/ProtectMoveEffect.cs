using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>
    /// Protect move effect: attaches the <c>protect</c> volatile (single-turn) to the user.
    /// PS's consecutive-use halving probability is intentionally skipped for now — it'll land
    /// when last-move tracking arrives (needed for Encore/Disable/Mimic too).
    /// </summary>
    public sealed class ProtectMoveEffect : Effect
    {
        public override string EffectId => "protectmove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            if (ev.User == null || ev.User.IsFainted) return;
            ev.Battle.AddVolatile(ev.User, "protect", singleTurn: true);
        }
    }
}
