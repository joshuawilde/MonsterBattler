using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Aurora Veil only sets up while Snow is active.</summary>
    public sealed class AuroraVeilMoveEffect : Effect
    {
        public override string EffectId => "auroraveilmove";
        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            if (ev.Battle.Field.Weather != Weather.Snow)
            {
                ev.Battle.Log.Raw($"|-fail|{ev.User.Species?.Name ?? ev.User.Nickname}|move: Aurora Veil");
                return;
            }
            var side = ev.Battle.SideOf(ev.User);
            if (side == null) return;
            var cond = ev.Battle.AddSideCondition(side, "auroraveil", maxLayers: 1, turns: 5);
            if (cond != null) cond.TurnsLeft = 5;
        }
    }
}
