using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Items
{
    /// <summary>Choice Specs: ×1.5 SpA, locks into first move.</summary>
    public sealed class ChoiceSpecsEffect : Effect
    {
        public override string EffectId => "choicespecs";
        public override string DisplayName => "Choice Specs";

        public override void OnModifySpA(StatModifyEvent ev, Pokemon owner)
        {
            if (owner != ev.Owner) return;
            ev.Value = ev.Value * 3 / 2;
        }

        public override void OnBeforeMove(BeforeMoveEvent ev, Pokemon owner)
        {
            if (owner != ev.User || string.IsNullOrEmpty(owner.LockedMoveId)) return;
            if (ev.Move.Id == owner.LockedMoveId) return;
            ev.Battle.Log.Raw($"|cant|{owner.Species?.Name ?? owner.Nickname}|Choice item|{owner.LockedMoveId}");
            ev.Cancelled = true;
        }

        public override void OnAfterMove(AfterMoveEvent ev, Pokemon owner)
        {
            if (owner != ev.User) return;
            if (string.IsNullOrEmpty(owner.LockedMoveId) && ev.Move != null)
                owner.LockedMoveId = ev.Move.Id;
        }
    }
}
