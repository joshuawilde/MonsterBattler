using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>
    /// Sleep Powder / Spore: applies Sleep to the target. Powder-type moves miss Grass-type
    /// targets (gen 6+). Spore is identical mechanically with 100% accuracy and Grass immunity.
    /// </summary>
    public sealed class SleepPowderMoveEffect : Effect
    {
        public override string EffectId => "sleeppowdermove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var t = ev.Target;
            if (t == null || t.IsFainted) return;
            if (IsType(t, MonType.Grass))
            {
                ev.Battle.Log.Raw($"|-immune|{Name(t)}|[from] Grass");
                return;
            }
            if (t.Status != StatusCondition.None)
            {
                ev.Battle.Log.Raw($"|-fail|{Name(t)}");
                return;
            }
            ev.Battle.ApplyStatus(t, StatusCondition.Sleep);
        }

        static bool IsType(Pokemon m, MonType t) => m?.Species != null && (m.Species.Type1 == t || m.Species.Type2 == t);
        static string Name(Pokemon m) => m?.Species?.Name ?? m?.Nickname ?? "?";
    }
}
