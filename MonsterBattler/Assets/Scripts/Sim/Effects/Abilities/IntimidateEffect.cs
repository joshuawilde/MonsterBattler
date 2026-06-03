using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Intimidate: when owner switches in, lower each opposing active Pokemon's Atk by 1 stage.
    /// </summary>
    public sealed class IntimidateEffect : Effect
    {
        public override string EffectId => "intimidate";
        public override string DisplayName => "Intimidate";

        public override void OnSwitchIn(SwitchInEvent ev, Pokemon owner)
        {
            if (owner != ev.Pokemon) return;
            var opp = ev.Battle.OpposingSideOf(owner);
            if (opp == null) return;
            foreach (var foe in opp.ActiveSlots)
            {
                if (foe == null || foe.IsFainted) continue;
                ev.Battle.BoostStat(foe, Stat.Atk, -1);
            }
        }
    }
}
