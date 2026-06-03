using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Adaptability: STAB becomes ×2 instead of ×1.5 (multiplies by an extra 4/3).</summary>
    public sealed class AdaptabilityEffect : Effect
    {
        public override string EffectId => "adaptability";
        public override string DisplayName => "Adaptability";

        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (owner != ev.User || ev.Move == null || owner.Species == null) return;
            if (ev.Move.Type != owner.Species.Type1 && ev.Move.Type != owner.Species.Type2) return;
            ev.Damage = ev.Damage * 4 / 3;
        }
    }
}
