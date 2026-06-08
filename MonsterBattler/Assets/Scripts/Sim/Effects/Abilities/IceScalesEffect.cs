using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Ice Scales: halves damage taken from special moves.</summary>
    public sealed class IceScalesEffect : Effect
    {
        public override string EffectId => "icescales";
        public override string DisplayName => "Ice Scales";

        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move == null) return;
            if (ev.Move.Category != MoveCategory.Special) return;
            ev.Damage /= 2;
        }
    }
}
