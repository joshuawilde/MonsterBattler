using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Parting Shot: lowers the target's Atk and SpA by 1, then the user switches out (pivotsOut).</summary>
    public sealed class PartingShotMoveEffect : Effect
    {
        public override string EffectId => "partingshotmove";
        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var t = ev.Target;
            if (t == null || t.IsFainted) return;
            ev.Battle.BoostStat(t, Stat.Atk, -1, source: ev.User);
            ev.Battle.BoostStat(t, Stat.SpA, -1, source: ev.User);
        }
    }
}
