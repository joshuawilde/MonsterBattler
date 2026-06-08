using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Cursed Body: 30% chance that a move which hits the owner becomes disabled for the attacker.
    /// </summary>
    public sealed class CursedBodyEffect : Effect
    {
        public override string EffectId => "cursedbody";
        public override string DisplayName => "Cursed Body";

        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            if (ev.User == null || ev.User.IsFainted) return;
            if (ev.Move == null) return;
            if (ev.User.Volatiles.ContainsKey("disable")) return;
            if (!ev.Battle.Prng.Chance(3, 10)) return;

            var slot = ev.Battle.AddVolatile(ev.User, "disable", source: owner);
            if (slot != null)
            {
                slot.Extra = ev.Move.Id;
                slot.Turns = 4;
            }
        }
    }
}
