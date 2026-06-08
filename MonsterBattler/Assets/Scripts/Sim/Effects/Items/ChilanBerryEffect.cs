using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Items
{
    /// <summary>Chilan Berry: halves any Normal-type hit, then is consumed.</summary>
    public sealed class ChilanBerryEffect : Effect
    {
        public override string EffectId => "chilanberry";
        public override string DisplayName => "Chilan Berry";
        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (ev.Battle.BerriesSuppressed(owner)) return;
            if (owner != ev.Target || ev.Move == null || ev.Move.Type != MonType.Normal || ev.Move.Category == MoveCategory.Status) return;
            ev.Damage = System.Math.Max(1, ev.Damage / 2);
            ev.Battle.ConsumeItem(owner, "item: Chilan Berry");
        }
    }
}
