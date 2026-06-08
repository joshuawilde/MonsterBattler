using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Items
{
    /// <summary>Payapa Berry: halves a super-effective Psychic-type hit, then is consumed.</summary>
    public sealed class PayapaberryEffect : Effect
    {
        public override string EffectId => "payapaberry";
        public override string DisplayName => "Payapa Berry";
        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (ev.Battle.BerriesSuppressed(owner)) return;
            if (owner != ev.Target || ev.Move == null || ev.Move.Category == MoveCategory.Status) return;
            if (ev.Move.Type != MonType.Psychic) return;
            var (t1, t2) = owner.CurrentTypes();
            if (TypeChart.Effectiveness(ev.Move.Type, t1, t2) <= 1f) return;
            ev.Damage = System.Math.Max(1, ev.Damage / 2);
            ev.Battle.ConsumeItem(owner, "item: Payapa Berry");
        }
    }
}
