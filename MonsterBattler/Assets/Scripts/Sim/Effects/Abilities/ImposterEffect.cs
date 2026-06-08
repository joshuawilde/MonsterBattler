using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Imposter: transforms into the opposing Pokemon on switch-in.</summary>
    public sealed class ImposterEffect : Effect
    {
        public override string EffectId => "imposter";
        public override string DisplayName => "Imposter";
        public override void OnSwitchIn(SwitchInEvent ev, Pokemon owner)
        {
            if (owner != ev.Pokemon || owner.Tags.Contains("transformed")) return;
            var opp = ev.Battle.OpposingSideOf(owner);
            var foe = opp != null && opp.ActiveSlots.Count > 0 ? opp.ActiveSlots[0] : null;
            if (foe != null && !foe.IsFainted) ev.Battle.Transform(owner, foe);
        }
    }
}
