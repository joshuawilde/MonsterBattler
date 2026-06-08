using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Cute Charm: 30% chance to infatuate attackers that make contact.</summary>
    public sealed class CuteCharmEffect : Effect
    {
        public override string EffectId => "cutecharm";
        public override string DisplayName => "Cute Charm";
        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move == null || !ev.Move.Contact) return;
            if (ev.User == null || ev.User.IsFainted || ev.User.GetVolatile("attract") != null) return;
            if (ev.Battle.Prng.Chance(3, 10)) ev.Battle.AddVolatile(ev.User, "attract", owner);
        }
    }
}
