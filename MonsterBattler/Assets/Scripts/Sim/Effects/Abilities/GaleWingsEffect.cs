using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Gale Wings: Flying-type moves gain +1 priority while the owner is at full HP.</summary>
    public sealed class GaleWingsEffect : Effect
    {
        public override string EffectId => "galewings";
        public override string DisplayName => "Gale Wings";
        public override void OnModifyPriority(ModifyPriorityEvent ev, Pokemon owner)
        {
            if (owner == ev.User && ev.Move?.Type == MonType.Flying &&
                owner.CurrentHp == owner.MaxStats[(int)Stat.HP]) ev.Priority += 1;
        }
    }
}
