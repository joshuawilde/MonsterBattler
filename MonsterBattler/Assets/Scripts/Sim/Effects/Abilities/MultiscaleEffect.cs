using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Multiscale: halves damage taken while owner is at full HP.</summary>
    public sealed class MultiscaleEffect : Effect
    {
        public override string EffectId => "multiscale";
        public override string DisplayName => "Multiscale";

        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            if (owner.CurrentHp != owner.MaxStats[(int)Stat.HP]) return;
            ev.Damage /= 2;
        }
    }
}
