using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Status moves whose only job is to paralyze (Glare, Stun Spore). Electric types are
    /// immune to paralysis; a mon that already has a status can't be re-statused.</summary>
    public sealed class ParalyzeMoveEffect : Effect
    {
        public override string EffectId => "paralyzemove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var t = ev.Target;
            if (t == null || t.IsFainted) return;
            if (IsType(t, MonType.Electric)) { ev.Battle.Log.Raw($"|-immune|{Name(t)}|[from] type"); return; }
            if (t.Status != StatusCondition.None) { ev.Battle.Log.Raw($"|-fail|{Name(t)}"); return; }
            ev.Battle.ApplyStatus(t, StatusCondition.Paralysis);
        }

        static bool IsType(Pokemon m, MonType t) => m?.Species != null && (m.Species.Type1 == t || m.Species.Type2 == t);
        static string Name(Pokemon m) => m?.Species?.Name ?? m?.Nickname ?? "?";
    }
}
