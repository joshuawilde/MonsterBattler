using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Zero to Hero (Palafin): on switching out, transform from Zero form to Hero form so the
    /// next time this mon is brought back, it's in Hero form. One-way transformation per battle.
    /// </summary>
    public sealed class ZeroToHeroEffect : Effect
    {
        public override string EffectId => "zerotohero";
        public override string DisplayName => "Zero to Hero";

        public override void OnSwitchOut(SwitchOutEvent ev, Pokemon owner)
        {
            if (owner != ev.Pokemon || owner.IsFainted) return;
            if (owner.Species?.Id != "palafin") return;
            ev.Battle.ChangeForm(owner, "palafinhero");
        }
    }
}
