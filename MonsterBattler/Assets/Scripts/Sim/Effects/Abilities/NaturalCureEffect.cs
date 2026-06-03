using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Natural Cure: on switch out, clear the owner's non-volatile status.</summary>
    public sealed class NaturalCureEffect : Effect
    {
        public override string EffectId => "naturalcure";
        public override string DisplayName => "Natural Cure";

        public override void OnSwitchOut(SwitchOutEvent ev, Pokemon owner)
        {
            if (owner != ev.Pokemon || owner.Status == StatusCondition.None) return;
            ev.Battle.Log.Raw($"|-curestatus|{owner.Species?.Name ?? owner.Nickname}|{owner.Status.ToString().ToLower()}|[from] ability: Natural Cure");
            owner.Status = StatusCondition.None;
            owner.StatusEffect = null;
            owner.ToxicCounter = 0;
            owner.SleepTurnsLeft = 0;
        }
    }
}
