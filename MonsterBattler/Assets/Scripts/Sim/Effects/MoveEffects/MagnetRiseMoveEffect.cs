using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Magnet Rise: the user gains Ground immunity for 5 turns (magnetrise volatile).</summary>
    public sealed class MagnetRiseMoveEffect : Effect
    {
        public override string EffectId => "magnetrisemove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var u = ev.User;
            if (u == null || u.IsFainted) return;
            if (u.GetVolatile("magnetrise") != null) { ev.Battle.Log.Raw($"|-fail|{Name(u)}"); return; }
            ev.Battle.AddVolatile(u, "magnetrise", u, turns: 5);
            ev.Battle.Log.Raw($"|-start|{Name(u)}|move: Magnet Rise");
        }

        static string Name(Pokemon m) => m?.Species?.Name ?? m?.Nickname ?? "?";
    }
}
