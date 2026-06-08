using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Poison Touch: 30% chance to poison the target when the owner makes contact with an attack.
    /// </summary>
    public sealed class PoisonTouchEffect : Effect
    {
        public override string EffectId => "poisontouch";
        public override string DisplayName => "Poison Touch";

        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.User) return;
            if (ev.Move == null || !ev.Move.Contact) return;
            if (ev.Target == null || ev.Target.IsFainted) return;
            if (ev.Target.Status != StatusCondition.None) return;
            if (!ev.Battle.Prng.Chance(3, 10)) return;
            ev.Battle.ApplyStatus(ev.Target, StatusCondition.Poison);
        }
    }
}
