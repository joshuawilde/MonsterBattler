using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.SideConditions
{
    /// <summary>
    /// Toxic Spikes: poison on switch-in. 1 layer = regular poison; 2 layers = badly poisoned.
    /// Flying types and Levitate users are unaffected. Poison types absorb the spikes when
    /// they switch in (removing the condition from their side).
    /// </summary>
    public sealed class ToxicSpikesCondition : Effect
    {
        public override string EffectId => "toxicspikes";
        public override string DisplayName => "Toxic Spikes";

        public override void OnSwitchIn(SwitchInEvent ev, Pokemon owner)
        {
            var mon = owner;
            if (mon == null || mon.IsFainted || mon.Species == null) return;
            if (mon.HasItem("heavydutyboots")) return;
            if (IsType(mon, MonType.Flying)) return;
            if (mon.AbilityEffect is Abilities.LevitateEffect) return;

            var side = ev.Battle.SideOf(mon);
            if (side == null) return;

            if (IsType(mon, MonType.Poison))
            {
                ev.Battle.RemoveSideCondition(side, "toxicspikes");
                return;
            }
            if (IsType(mon, MonType.Steel)) return;
            if (mon.Status != StatusCondition.None) return;

            if (!side.Conditions.TryGetValue("toxicspikes", out var cond)) return;
            if (cond.Layers >= 2) ev.Battle.ApplyStatus(mon, StatusCondition.BadlyPoisoned);
            else ev.Battle.ApplyStatus(mon, StatusCondition.Poison);
        }

        static bool IsType(Pokemon m, MonType t) => m?.Species != null && (m.Species.Type1 == t || m.Species.Type2 == t);
    }
}
