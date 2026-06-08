using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Prism Armor: takes 0.75× damage from super-effective hits (ignores other abilities, but we treat it as Filter).</summary>
    public sealed class PrismArmorEffect : Effect
    {
        public override string EffectId => "prismarmor";
        public override string DisplayName => "Prism Armor";
        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move == null || owner.Species == null) return;
            float eff = TypeChart.Effectiveness(ev.Move.Type, owner.Species.Type1, owner.Species.Type2);
            if (eff > 1f) ev.Damage = ev.Damage * 3 / 4;
        }
    }
}
