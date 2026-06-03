using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Volatiles
{
    /// <summary>
    /// Encore: forces target to repeat their last move for 3 turns. Implemented by setting
    /// LockedMoveId — the UI / AI honors that, and BeforeMove enforces it as a fallback.
    /// </summary>
    public sealed class EncoreVolatile : Effect
    {
        public override string EffectId => "encore";
        public override string DisplayName => "Encore";

        public override void OnBeforeMove(BeforeMoveEvent ev, Pokemon owner)
        {
            if (owner != ev.User) return;
            var slot = owner.GetVolatile("encore");
            if (slot == null) return;
            string lockedId = slot.Extra as string;
            if (string.IsNullOrEmpty(lockedId)) return;
            if (ev.Move?.Id != lockedId)
            {
                ev.Battle.Log.Raw($"|cant|{owner.Species?.Name ?? owner.Nickname}|encore");
                ev.Cancelled = true;
                return;
            }
            if (slot.Turns <= 0)
            {
                ev.Battle.RemoveVolatile(owner, "encore");
                if (owner.LockedMoveId == lockedId) owner.LockedMoveId = null;
                return;
            }
            slot.Turns--;
        }
    }
}
