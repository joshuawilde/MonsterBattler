using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Items
{
    /// <summary>Coba Berry: halves a super-effective Flying-type hit, then is consumed.</summary>
    public sealed class CobaberryEffect : Effect
    {
        public override string EffectId => "cobaberry";
        public override string DisplayName => "Coba Berry";
        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (ev.Battle.BerriesSuppressed(owner)) return;
            if (owner != ev.Target || ev.Move == null || ev.Move.Category == MoveCategory.Status) return;
            if (ev.Move.Type != MonType.Flying) return;
            var (t1, t2) = owner.CurrentTypes();
            if (TypeChart.Effectiveness(ev.Move.Type, t1, t2) <= 1f) return;
            ev.Damage = System.Math.Max(1, ev.Damage / 2);
            ev.Battle.ConsumeItem(owner, "item: Coba Berry");
        }
    }
}
