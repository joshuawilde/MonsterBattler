using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Yawn: applies the 'yawn' volatile, putting the target to sleep at the end of its
    /// next turn (handled by YawnVolatile.OnResidual).</summary>
    public sealed class YawnMoveEffect : Effect
    {
        public override string EffectId => "yawnmove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var t = ev.Target;
            if (t == null || t.IsFainted) return;
            if (t.Status != StatusCondition.None) { ev.Battle.Log.Raw($"|-fail|{Name(t)}"); return; }
            if (t.GetVolatile("yawn") != null) { ev.Battle.Log.Raw($"|-fail|{Name(t)}"); return; }
            ev.Battle.AddVolatile(t, "yawn", ev.User, turns: 2);
            ev.Battle.Log.Raw($"|-start|{Name(t)}|move: Yawn");
        }

        static string Name(Pokemon m) => m?.Species?.Name ?? m?.Nickname ?? "?";
    }
}
