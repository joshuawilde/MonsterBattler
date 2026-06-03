using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Tinted Lens: doubles damage of "not very effective" hits — turning 0.5× into 1× / 0.25× into 0.5×.</summary>
    public sealed class TintedLensEffect : Effect
    {
        public override string EffectId => "tintedlens";
        public override string DisplayName => "Tinted Lens";

        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (owner != ev.User || ev.Move == null || ev.Target?.Species == null) return;
            // Recompute type effectiveness against current defensive type set (handles Terastallization).
            MonType defType1 = ev.Target.IsTerastallized ? ev.Target.TeraType : ev.Target.Species.Type1;
            MonType defType2 = ev.Target.IsTerastallized ? MonType.None : ev.Target.Species.Type2;
            float eff = TypeChart.Effectiveness(ev.Move.Type, defType1, defType2);
            if (eff > 0f && eff < 1f) ev.Damage *= 2;
        }
    }
}
