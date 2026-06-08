using MonsterBattler.Sim.Events;
using MonsterBattler.Sim.Data;

namespace MonsterBattler.Sim.Effects.Volatiles
{
    /// <summary>Taunt: the owner can't select Status-category moves for a few turns.</summary>
    public sealed class TauntVolatile : Effect
    {
        public override string EffectId => "taunt";
        public override string DisplayName => "Taunt";

        public override void OnBeforeMove(BeforeMoveEvent ev, Pokemon owner)
        {
            if (owner != ev.User) return;
            var slot = owner.GetVolatile("taunt");
            if (slot == null) return;
            if (slot.Turns <= 0) { ev.Battle.RemoveVolatile(owner, "taunt"); return; }
            slot.Turns--;
            if (ev.Move != null && ev.Move.Category == MoveCategory.Status)
            {
                ev.Battle.Log.Raw($"|cant|{owner.Species?.Name ?? owner.Nickname}|move: Taunt|{ev.Move.Name}");
                ev.Cancelled = true;
            }
        }
    }
}
