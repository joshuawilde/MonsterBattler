using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>
    /// Bind / Wrap / Whirlpool / Fire Spin / Magma Storm / Sand Tomb / Infestation: damage
    /// goes through normal calc, then a 4-5 turn trapping volatile is attached to the target.
    /// </summary>
    public sealed class TrappingMoveEffect : Effect
    {
        public override string EffectId => "trappingmove";

        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            var t = ev.Target;
            if (t == null || t.IsFainted) return;
            if (t.Volatiles.ContainsKey("partiallytrapped")) return;
            var slot = ev.Battle.AddVolatile(t, "partiallytrapped", source: ev.User);
            // Gen 5+: 4-5 turns (Grip Claw extends to 7 — skip until items).
            if (slot != null) slot.Turns = ev.Battle.Prng.Range(4, 6);
        }
    }
}
