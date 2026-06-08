using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Taunt: applies the taunt volatile to the target (3 turns).</summary>
    public sealed class TauntMoveEffect : Effect
    {
        public override string EffectId => "tauntmove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var t = ev.Target;
            if (t == null || t.IsFainted) return;
            if (t.GetVolatile("taunt") != null) { ev.Battle.Log.Raw($"|-fail|{Name(t)}"); return; }
            ev.Battle.AddVolatile(t, "taunt", ev.User, turns: 3);
            ev.Battle.Log.Raw($"|-start|{Name(t)}|move: Taunt");
        }

        static string Name(Pokemon m) => m?.Species?.Name ?? m?.Nickname ?? "?";
    }
}
