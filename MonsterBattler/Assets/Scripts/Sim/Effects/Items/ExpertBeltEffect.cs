using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Items
{
    /// <summary>Expert Belt: ×1.2 damage on super-effective moves.</summary>
    public sealed class ExpertBeltEffect : Effect
    {
        public override string EffectId => "expertbelt";
        public override string DisplayName => "Expert Belt";
        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (owner != ev.User || ev.Move == null || ev.Target == null || ev.Move.Category == MoveCategory.Status) return;
            var (t1, t2) = ev.Target.CurrentTypes();
            if (TypeChart.Effectiveness(ev.Move.Type, t1, t2) > 1f) ev.Damage = ev.Damage * 6 / 5;
        }
    }
}
