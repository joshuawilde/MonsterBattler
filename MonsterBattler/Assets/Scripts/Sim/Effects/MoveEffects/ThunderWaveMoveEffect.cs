using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    public sealed class ThunderWaveMoveEffect : Effect
    {
        public override string EffectId => "thunderwavemove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var t = ev.Target;
            if (t == null || t.IsFainted) return;
            // Ground types are immune to the move itself (Electric → Ground = 0× per type chart);
            // gen 6+ Electric types are immune to paralysis from any source.
            if (IsType(t, MonType.Ground))
            {
                ev.Battle.Log.Raw($"|-immune|{Name(t)}");
                return;
            }
            if (IsType(t, MonType.Electric))
            {
                ev.Battle.Log.Raw($"|-immune|{Name(t)}|[from] type");
                return;
            }
            if (t.Status != StatusCondition.None)
            {
                ev.Battle.Log.Raw($"|-fail|{Name(t)}");
                return;
            }
            ev.Battle.ApplyStatus(t, StatusCondition.Paralysis);
        }

        static bool IsType(Pokemon m, MonType t) => m?.Species != null && (m.Species.Type1 == t || m.Species.Type2 == t);
        static string Name(Pokemon m) => m?.Species?.Name ?? m?.Nickname ?? "?";
    }
}
