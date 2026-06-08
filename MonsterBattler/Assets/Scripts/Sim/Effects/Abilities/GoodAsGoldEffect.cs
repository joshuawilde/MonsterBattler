using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Good as Gold: the owner is immune to status-category moves used against it.
    /// </summary>
    public sealed class GoodAsGoldEffect : Effect
    {
        public override string EffectId => "goodasgold";
        public override string DisplayName => "Good as Gold";

        public override void OnTryHit(TryHitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            if (ev.User == owner) return;
            if (ev.Move?.Category != MoveCategory.Status) return;
            ev.Blocked = true;
            ev.BlockReason = "Good as Gold";
        }
    }
}
