using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Competitive: sharply raises SpA (+2) when a stat is lowered by an opposing Pokemon.</summary>
    public sealed class CompetitiveEffect : Effect
    {
        public override string EffectId => "competitive";
        public override string DisplayName => "Competitive";
        public override void OnAfterStatLowered(StatModifyEvent ev, Pokemon owner)
        {
            if (owner != ev.Owner) return;
            ev.Battle.BoostStat(owner, Stat.SpA, +2, source: owner);
        }
    }
}
