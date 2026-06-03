using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    public sealed class ConfuseRayMoveEffect : Effect
    {
        public override string EffectId => "confuseraymove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var t = ev.Target;
            if (t == null || t.IsFainted) return;
            if (t.Volatiles.ContainsKey("confusion"))
            {
                ev.Battle.Log.Raw($"|-fail|{t.Species?.Name ?? t.Nickname}|confusion");
                return;
            }
            var slot = ev.Battle.AddVolatile(t, "confusion");
            if (slot != null) slot.Turns = ev.Battle.Prng.Range(2, 6); // 2..5 inclusive — match PS turn pool
        }
    }
}
