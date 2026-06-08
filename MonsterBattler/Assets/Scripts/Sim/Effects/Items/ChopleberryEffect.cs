using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Items
{
    /// <summary>Chople Berry: halves a super-effective Fighting-type hit, then is consumed.</summary>
    public sealed class ChopleberryEffect : Effect
    {
        public override string EffectId => "chopleberry";
        public override string DisplayName => "Chople Berry";
        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (ev.Battle.BerriesSuppressed(owner)) return;
            if (owner != ev.Target || ev.Move == null || ev.Move.Category == MoveCategory.Status) return;
            if (ev.Move.Type != MonType.Fighting) return;
            var (t1, t2) = owner.CurrentTypes();
            if (TypeChart.Effectiveness(ev.Move.Type, t1, t2) <= 1f) return;
            ev.Damage = System.Math.Max(1, ev.Damage / 2);
            ev.Battle.ConsumeItem(owner, "item: Chople Berry");
        }
    }
}
