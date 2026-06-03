using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>
    /// Leech Seed move: applies the <c>leechseed</c> volatile to the target with the user
    /// recorded as the seeder. Fails against Grass-types and against an already-seeded target.
    /// </summary>
    public sealed class LeechSeedMoveEffect : Effect
    {
        public override string EffectId => "leechseedmove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var target = ev.Target;
            if (target == null || target.IsFainted) return;
            if (IsType(target, MonType.Grass))
            {
                ev.Battle.Log.Raw($"|-immune|{target.Species?.Name ?? target.Nickname}|[from] Grass");
                return;
            }
            if (target.Volatiles.ContainsKey("leechseed"))
            {
                ev.Battle.Log.Raw($"|-fail|{target.Species?.Name ?? target.Nickname}|leechseed");
                return;
            }
            ev.Battle.AddVolatile(target, "leechseed", source: ev.User);
        }

        static bool IsType(Pokemon mon, MonType t)
        {
            if (mon?.Species == null) return false;
            return mon.Species.Type1 == t || mon.Species.Type2 == t;
        }
    }
}
