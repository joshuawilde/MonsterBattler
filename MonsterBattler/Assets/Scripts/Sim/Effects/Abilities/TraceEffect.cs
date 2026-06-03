using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Trace: on switch in, copy the opposing active mon's ability. Doesn't copy Trace itself.
    /// </summary>
    public sealed class TraceEffect : Effect
    {
        public override string EffectId => "trace";
        public override string DisplayName => "Trace";

        public override void OnSwitchIn(SwitchInEvent ev, Pokemon owner)
        {
            if (owner != ev.Pokemon) return;
            var opp = ev.Battle.OpposingSideOf(owner);
            if (opp == null || opp.ActiveSlots.Count == 0) return;
            var foe = opp.ActiveSlots[0];
            if (foe == null || foe.IsFainted || foe.AbilityEffect == null) return;
            if (foe.AbilityEffect is TraceEffect) return; // can't trace Trace
            owner.Ability = foe.Ability;
            owner.AbilityEffect = foe.AbilityEffect;
            ev.Battle.Log.Raw($"|-ability|{owner.Species?.Name ?? owner.Nickname}|{foe.AbilityEffect.DisplayName}|[from] ability: Trace");
        }
    }
}
