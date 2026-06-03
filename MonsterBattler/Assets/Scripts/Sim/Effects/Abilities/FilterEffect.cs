using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Filter / Solid Rock / Prism Armor: ×0.75 damage from super-effective hits.</summary>
    public sealed class FilterEffect : Effect
    {
        public override string EffectId => "filter";
        public override string DisplayName => "Filter";

        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move == null || owner.Species == null) return;
            float eff = TypeChart.Effectiveness(ev.Move.Type, owner.Species.Type1, owner.Species.Type2);
            if (eff <= 1f) return;
            ev.Damage = ev.Damage * 3 / 4;
        }
    }
}
