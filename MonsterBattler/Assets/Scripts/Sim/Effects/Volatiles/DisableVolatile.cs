using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Volatiles
{
    /// <summary>Disable: forbids one move (stored in <see cref="VolatileSlot.Extra"/>) for 4 turns.</summary>
    public sealed class DisableVolatile : Effect
    {
        public override string EffectId => "disable";
        public override string DisplayName => "Disable";

        public override void OnBeforeMove(BeforeMoveEvent ev, Pokemon owner)
        {
            if (owner != ev.User) return;
            var slot = owner.GetVolatile("disable");
            if (slot == null) return;
            if (slot.Turns <= 0)
            {
                ev.Battle.RemoveVolatile(owner, "disable");
                return;
            }
            slot.Turns--;
            string bannedId = slot.Extra as string;
            if (ev.Move?.Id == bannedId)
            {
                ev.Battle.Log.Raw($"|cant|{owner.Species?.Name ?? owner.Nickname}|Disable|{ev.Move.Name}");
                ev.Cancelled = true;
            }
        }
    }
}
