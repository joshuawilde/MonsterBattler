using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Effect Spore: 30% chance (split poison/paralysis/sleep) on contact.</summary>
    public sealed class EffectSporeEffect : Effect
    {
        public override string EffectId => "effectspore";
        public override string DisplayName => "Effect Spore";
        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move == null || !ev.Move.Contact) return;
            if (ev.User == null || ev.User.IsFainted || ev.User.Status != StatusCondition.None) return;
            int roll = ev.Battle.Prng.Range(0, 10); // 0..2 poison, 3..5 para, 6..8 sleep, 9 nothing (~30%)
            if (roll < 3) ev.Battle.ApplyStatus(ev.User, StatusCondition.Poison);
            else if (roll < 6) ev.Battle.ApplyStatus(ev.User, StatusCondition.Paralysis);
            else if (roll < 9) ev.Battle.ApplyStatus(ev.User, StatusCondition.Sleep);
        }
    }
}
