using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Prankster: the owner's Status-category moves gain +1 priority.</summary>
    public sealed class PranksterEffect : Effect
    {
        public override string EffectId => "prankster";
        public override string DisplayName => "Prankster";
        public override void OnModifyPriority(ModifyPriorityEvent ev, Pokemon owner)
        {
            if (owner == ev.User && ev.Move?.Category == MoveCategory.Status) ev.Priority += 1;
        }
    }
}
