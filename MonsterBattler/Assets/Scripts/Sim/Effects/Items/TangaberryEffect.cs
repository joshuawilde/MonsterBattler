using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Items
{
    /// <summary>Tanga Berry: halves a super-effective Bug-type hit, then is consumed.</summary>
    public sealed class TangaberryEffect : Effect
    {
        public override string EffectId => "tangaberry";
        public override string DisplayName => "Tanga Berry";
        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (ev.Battle.BerriesSuppressed(owner)) return;
            if (owner != ev.Target || ev.Move == null || ev.Move.Category == MoveCategory.Status) return;
            if (ev.Move.Type != MonType.Bug) return;
            var (t1, t2) = owner.CurrentTypes();
            if (TypeChart.Effectiveness(ev.Move.Type, t1, t2) <= 1f) return;
            ev.Damage = System.Math.Max(1, ev.Damage / 2);
            ev.Battle.ConsumeItem(owner, "item: Tanga Berry");
        }
    }
}
