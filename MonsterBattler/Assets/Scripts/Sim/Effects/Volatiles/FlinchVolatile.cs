using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Volatiles
{
    /// <summary>
    /// Flinch: cancels the owner's move this turn. Added as a single-turn volatile, so end-of-turn
    /// cleanup removes it automatically — flinching only matters when the attacker outsped.
    /// </summary>
    public sealed class FlinchVolatile : Effect
    {
        public override string EffectId => "flinch";
        public override string DisplayName => "Flinch";

        public override void OnBeforeMove(BeforeMoveEvent ev, Pokemon owner)
        {
            if (owner != ev.User) return;
            ev.Battle.Log.Raw($"|cant|{owner.Species?.Name ?? owner.Nickname}|flinch");
            ev.Cancelled = true;
        }
    }
}
