using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Hydration: cures the owner's non-volatile status at the end of the turn in rain.</summary>
    public sealed class HydrationEffect : Effect
    {
        public override string EffectId => "hydration";
        public override string DisplayName => "Hydration";

        public override void OnResidual(ResidualEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || owner.IsFainted) return;
            if (owner.Status == StatusCondition.None) return;
            if (ev.Battle.Field.Weather != Weather.Rain) return;
            ev.Battle.Log.Raw($"|-curestatus|{owner.Species?.Name ?? owner.Nickname}|{owner.Status.ToString().ToLower()}|[from] ability: Hydration");
            owner.Status = StatusCondition.None;
            owner.StatusEffect = null;
            owner.ToxicCounter = 0;
            owner.SleepTurnsLeft = 0;
        }
    }
}
