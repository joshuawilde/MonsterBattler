using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Trick / Switcheroo: swap held items with target. Fails if either side has no item.</summary>
    public sealed class TrickMoveEffect : Effect
    {
        public override string EffectId => "trickmove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var u = ev.User; var t = ev.Target;
            if (u == null || t == null || u.IsFainted || t.IsFainted) return;
            if (u.Item == null && t.Item == null)
            {
                ev.Battle.Log.Raw($"|-fail|{u.Species?.Name ?? u.Nickname}");
                return;
            }
            (u.Item, t.Item) = (t.Item, u.Item);
            (u.ItemEffect, t.ItemEffect) = (t.ItemEffect, u.ItemEffect);
            // Clear Choice locks — the lock is item-tied.
            u.LockedMoveId = null;
            t.LockedMoveId = null;
            ev.Battle.Log.Raw($"|-item|{u.Species?.Name ?? u.Nickname}|{u.Item?.Name ?? "(none)"}|[from] move: Trick");
            ev.Battle.Log.Raw($"|-item|{t.Species?.Name ?? t.Nickname}|{t.Item?.Name ?? "(none)"}|[from] move: Trick");
        }
    }
}
