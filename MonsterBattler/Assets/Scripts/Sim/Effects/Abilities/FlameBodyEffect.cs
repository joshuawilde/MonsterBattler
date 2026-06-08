using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Flame Body: 30% chance to burn attackers that make contact.</summary>
    public sealed class FlameBodyEffect : Effect
    {
        public override string EffectId => "flamebody";
        public override string DisplayName => "Flame Body";
        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move == null || !ev.Move.Contact) return;
            if (ev.User == null || ev.User.IsFainted || ev.User.Status != StatusCondition.None) return;
            if (!ev.Battle.Prng.Chance(3, 10)) return;
            ev.Battle.ApplyStatus(ev.User, StatusCondition.Burn);
        }
    }
}
