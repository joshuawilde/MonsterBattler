using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Thick Fat: halves Fire and Ice damage taken.</summary>
    public sealed class ThickFatEffect : Effect
    {
        public override string EffectId => "thickfat";
        public override string DisplayName => "Thick Fat";

        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move == null) return;
            if (ev.Move.Type != MonType.Fire && ev.Move.Type != MonType.Ice) return;
            ev.Damage /= 2;
        }
    }
}
