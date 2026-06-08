using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Items
{
    /// <summary>Muscle Band: ×1.1 damage on physical moves.</summary>
    public sealed class MuscleBandEffect : Effect
    {
        public override string EffectId => "muscleband";
        public override string DisplayName => "Muscle Band";
        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (owner == ev.User && ev.Move?.Category == MoveCategory.Physical) ev.Damage = ev.Damage * 11 / 10;
        }
    }
}
