using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Items
{
    /// <summary>Wise Glasses: ×1.1 damage on special moves.</summary>
    public sealed class WiseGlassesEffect : Effect
    {
        public override string EffectId => "wiseglasses";
        public override string DisplayName => "Wise Glasses";
        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (owner == ev.User && ev.Move?.Category == MoveCategory.Special) ev.Damage = ev.Damage * 11 / 10;
        }
    }
}
