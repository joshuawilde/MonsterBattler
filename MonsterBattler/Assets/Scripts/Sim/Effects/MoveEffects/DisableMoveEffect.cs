using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    public sealed class DisableMoveEffect : Effect
    {
        public override string EffectId => "disablemove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var t = ev.Target;
            if (t == null || t.IsFainted || t.LastMoveUsed == null
                || t.Volatiles.ContainsKey("disable"))
            {
                ev.Battle.Log.Raw($"|-fail|{t?.Species?.Name ?? t?.Nickname}");
                return;
            }
            var slot = ev.Battle.AddVolatile(t, "disable", source: ev.User);
            if (slot != null)
            {
                slot.Extra = t.LastMoveUsed.Id;
                slot.Turns = 4;
            }
        }
    }
}
