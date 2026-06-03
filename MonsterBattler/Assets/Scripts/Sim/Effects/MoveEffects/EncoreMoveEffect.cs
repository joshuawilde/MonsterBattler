using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    public sealed class EncoreMoveEffect : Effect
    {
        public override string EffectId => "encoremove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var t = ev.Target;
            if (t == null || t.IsFainted || t.LastMoveUsed == null) { Fail(ev, t); return; }
            if (t.Volatiles.ContainsKey("encore")) { Fail(ev, t); return; }
            var slot = ev.Battle.AddVolatile(t, "encore", source: ev.User);
            if (slot != null)
            {
                slot.Extra = t.LastMoveUsed.Id;
                slot.Turns = 3;
                t.LockedMoveId = t.LastMoveUsed.Id;
            }
        }

        static void Fail(HitEvent ev, Pokemon t) =>
            ev.Battle.Log.Raw($"|-fail|{t?.Species?.Name ?? t?.Nickname}");
    }
}
