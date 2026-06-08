using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Items
{
    /// <summary>Babiri Berry: halves a super-effective Steel-type hit, then is consumed.</summary>
    public sealed class BabiriberryEffect : Effect
    {
        public override string EffectId => "babiriberry";
        public override string DisplayName => "Babiri Berry";
        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (ev.Battle.BerriesSuppressed(owner)) return;
            if (owner != ev.Target || ev.Move == null || ev.Move.Category == MoveCategory.Status) return;
            if (ev.Move.Type != MonType.Steel) return;
            var (t1, t2) = owner.CurrentTypes();
            if (TypeChart.Effectiveness(ev.Move.Type, t1, t2) <= 1f) return;
            ev.Damage = System.Math.Max(1, ev.Damage / 2);
            ev.Battle.ConsumeItem(owner, "item: Babiri Berry");
        }
    }
}
