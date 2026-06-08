using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Status moves whose only job is to put the target to sleep (Hypnosis, Spore,
    /// Sleep Powder, Sing, Lovely Kiss). A mon that already has a status can't be re-statused.</summary>
    public sealed class SleepMoveEffect : Effect
    {
        public override string EffectId => "sleepmove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var t = ev.Target;
            if (t == null || t.IsFainted) return;
            if (t.Status != StatusCondition.None) { ev.Battle.Log.Raw($"|-fail|{Name(t)}"); return; }
            ev.Battle.ApplyStatus(t, StatusCondition.Sleep);
        }

        static string Name(Pokemon m) => m?.Species?.Name ?? m?.Nickname ?? "?";
    }
}
