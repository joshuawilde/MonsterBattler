using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.SideConditions
{
    /// <summary>Tailwind: doubles the side's Speed for 4 turns.</summary>
    public sealed class TailwindCondition : Effect
    {
        public override string EffectId => "tailwind";
        public override string DisplayName => "Tailwind";

        public override void OnModifySpe(StatModifyEvent ev, Pokemon owner)
        {
            // OnModifySpe is dispatched with owner = the affected mon. The condition only fires
            // when this mon is on the side that owns it (which is how DispatchSideOf invokes us).
            if (owner != ev.Owner) return;
            ev.Value *= 2;
        }
    }
}
