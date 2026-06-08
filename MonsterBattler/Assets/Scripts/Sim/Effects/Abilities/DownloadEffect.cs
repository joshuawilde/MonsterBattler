using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Download: on switch in, compare the opposing active mon's Def vs SpD. Raise the owner's
    /// Atk by 1 if the foe's Def is lower (or equal), otherwise raise SpA by 1.
    /// </summary>
    public sealed class DownloadEffect : Effect
    {
        public override string EffectId => "download";
        public override string DisplayName => "Download";

        public override void OnSwitchIn(SwitchInEvent ev, Pokemon owner)
        {
            if (owner != ev.Pokemon) return;
            var opp = ev.Battle.OpposingSideOf(owner);
            if (opp == null) return;

            int totalDef = 0, totalSpD = 0, count = 0;
            foreach (var foe in opp.ActiveSlots)
            {
                if (foe == null || foe.IsFainted) continue;
                totalDef += foe.MaxStats[(int)Stat.Def];
                totalSpD += foe.MaxStats[(int)Stat.SpD];
                count++;
            }
            if (count == 0) return;

            if (totalDef <= totalSpD)
                ev.Battle.BoostStat(owner, Stat.Atk, 1, source: owner);
            else
                ev.Battle.BoostStat(owner, Stat.SpA, 1, source: owner);
        }
    }
}
