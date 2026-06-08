using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Shadow Shield: halves damage taken while the owner is at full HP.</summary>
    public sealed class ShadowShieldEffect : Effect
    {
        public override string EffectId => "shadowshield";
        public override string DisplayName => "Shadow Shield";

        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            if (owner.CurrentHp != owner.MaxStats[(int)Stat.HP]) return;
            ev.Damage /= 2;
        }
    }
}
