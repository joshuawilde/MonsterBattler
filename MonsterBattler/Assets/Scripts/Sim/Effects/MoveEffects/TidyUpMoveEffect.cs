using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Tidy Up: raises user's Atk and Spe by 1 and clears hazards from BOTH sides.</summary>
    public sealed class TidyUpMoveEffect : Effect
    {
        public override string EffectId => "tidyupmove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var u = ev.User;
            if (u == null || u.IsFainted) return;

            foreach (var side in ev.Battle.Sides)
            {
                if (side == null) continue;
                ev.Battle.RemoveSideCondition(side, "stealthrock");
                ev.Battle.RemoveSideCondition(side, "spikes");
                ev.Battle.RemoveSideCondition(side, "toxicspikes");
                ev.Battle.RemoveSideCondition(side, "stickyweb");
            }

            ev.Battle.BoostStat(u, Stat.Atk, +1, u);
            ev.Battle.BoostStat(u, Stat.Spe, +1, u);
        }
    }
}
