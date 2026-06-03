using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    public sealed class ToxicMoveEffect : Effect
    {
        public override string EffectId => "toxicmove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var t = ev.Target;
            if (t == null || t.IsFainted) return;
            if (IsType(t, MonType.Steel) || IsType(t, MonType.Poison))
            {
                ev.Battle.Log.Raw($"|-immune|{Name(t)}");
                return;
            }
            if (t.Status != StatusCondition.None)
            {
                ev.Battle.Log.Raw($"|-fail|{Name(t)}");
                return;
            }
            ev.Battle.ApplyStatus(t, StatusCondition.BadlyPoisoned);
        }

        static bool IsType(Pokemon m, MonType t) => m?.Species != null && (m.Species.Type1 == t || m.Species.Type2 == t);
        static string Name(Pokemon m) => m?.Species?.Name ?? m?.Nickname ?? "?";
    }
}
