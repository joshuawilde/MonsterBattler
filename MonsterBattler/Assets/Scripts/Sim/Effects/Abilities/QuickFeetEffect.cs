using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Quick Feet: ×1.5 Speed while the owner has a non-volatile status condition.</summary>
    public sealed class QuickFeetEffect : Effect
    {
        public override string EffectId => "quickfeet";
        public override string DisplayName => "Quick Feet";

        public override void OnModifySpe(StatModifyEvent ev, Pokemon owner)
        {
            if (owner != ev.Owner) return;
            if (owner.Status == StatusCondition.None) return;
            ev.Value = ev.Value * 3 / 2;
        }
    }
}
