using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Static: 30% chance to paralyze attackers that make contact with the owner.
    /// </summary>
    public sealed class StaticEffect : Effect
    {
        public override string EffectId => "static";
        public override string DisplayName => "Static";

        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            if (ev.Move == null || !ev.Move.Contact) return;
            if (ev.User == null || ev.User.IsFainted) return;
            if (ev.User.Status != StatusCondition.None) return;
            if (!ev.Battle.Prng.Chance(3, 10)) return;
            ev.Battle.ApplyStatus(ev.User, StatusCondition.Paralysis);
        }
    }
}
