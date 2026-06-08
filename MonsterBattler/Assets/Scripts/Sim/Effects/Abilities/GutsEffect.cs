using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Guts: while the owner has a non-volatile status condition, its Attack is multiplied by 1.5×.
    /// (Burn's attack drop is also ignored in canon; here we only apply the 1.5× boost.)
    /// </summary>
    public sealed class GutsEffect : Effect
    {
        public override string EffectId => "guts";
        public override string DisplayName => "Guts";

        public override void OnModifyAtk(StatModifyEvent ev, Pokemon owner)
        {
            if (owner != ev.Owner) return;
            if (owner.Status == StatusCondition.None) return;
            ev.Value = ev.Value * 3 / 2;
        }
    }
}
