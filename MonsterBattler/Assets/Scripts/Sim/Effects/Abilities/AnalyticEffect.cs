using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Analytic: +30% base power if the user moves after its target this turn.</summary>
    public sealed class AnalyticEffect : Effect
    {
        public override string EffectId => "analytic";
        public override string DisplayName => "Analytic";
        public override void OnBasePower(BasePowerEvent ev, Pokemon owner)
        {
            if (owner == ev.User && ev.Target != null && ev.Target.ActedThisTurn)
                ev.BasePower = ev.BasePower * 13 / 10;
        }
    }
}
