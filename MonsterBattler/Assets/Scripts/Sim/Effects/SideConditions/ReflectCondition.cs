using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.SideConditions
{
    /// <summary>Reflect: halves incoming physical damage to the protected side. 5 turns. Skips on crits.</summary>
    public sealed class ReflectCondition : Effect
    {
        public override string EffectId => "reflect";
        public override string DisplayName => "Reflect";

        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.IsCrit || ev.Move == null) return;
            if (ev.User?.AbilityEffect is Abilities.InfiltratorEffect) return; // Infiltrator ignores screens
            if (ev.Move.Category != MoveCategory.Physical) return;
            ev.Damage /= 2;
        }
    }
}
