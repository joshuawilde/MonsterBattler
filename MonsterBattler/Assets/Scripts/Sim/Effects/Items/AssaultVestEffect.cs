using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Items
{
    /// <summary>Assault Vest: ×1.5 Special Defense, but the holder cannot use status moves.</summary>
    public sealed class AssaultVestEffect : Effect
    {
        public override string EffectId => "assaultvest";
        public override string DisplayName => "Assault Vest";

        public override void OnModifySpD(StatModifyEvent ev, Pokemon owner)
        {
            if (owner != ev.Owner) return;
            ev.Value = ev.Value * 3 / 2;
        }

        public override void OnBeforeMove(BeforeMoveEvent ev, Pokemon owner)
        {
            if (owner != ev.User || ev.Move == null || ev.Move.Category != MoveCategory.Status) return;
            ev.Battle.Log.Raw($"|cant|{owner.Species?.Name ?? owner.Nickname}|Assault Vest|{ev.Move.Id}");
            ev.Cancelled = true;
        }
    }
}
