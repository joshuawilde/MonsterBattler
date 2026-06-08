using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Volatiles
{
    /// <summary>Infatuation: 50% chance each turn the owner can't move.</summary>
    public sealed class AttractVolatile : Effect
    {
        public override string EffectId => "attract";
        public override string DisplayName => "Attract";
        public override void OnBeforeMove(BeforeMoveEvent ev, Pokemon owner)
        {
            if (owner != ev.User) return;
            if (ev.Battle.Prng.Chance(1, 2))
            {
                ev.Battle.Log.Raw($"|cant|{owner.Species?.Name ?? owner.Nickname}|Attract");
                ev.Cancelled = true;
            }
        }
    }
}
