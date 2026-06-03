using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.SideConditions
{
    /// <summary>Light Screen: halves incoming special damage to the protected side. 5 turns. Skips on crits.</summary>
    public sealed class LightScreenCondition : Effect
    {
        public override string EffectId => "lightscreen";
        public override string DisplayName => "Light Screen";

        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.IsCrit || ev.Move == null) return;
            if (ev.Move.Category != MoveCategory.Special) return;
            ev.Damage /= 2;
        }
    }
}
