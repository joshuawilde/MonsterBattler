using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.SideConditions
{
    /// <summary>Aurora Veil: halves both physical and special damage. 5 turns. Skips on crits. Snow-only to set up.</summary>
    public sealed class AuroraVeilCondition : Effect
    {
        public override string EffectId => "auroraveil";
        public override string DisplayName => "Aurora Veil";

        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.IsCrit || ev.Move == null) return;
            if (ev.Move.Category == MoveCategory.Status) return;
            ev.Damage /= 2;
        }
    }
}
