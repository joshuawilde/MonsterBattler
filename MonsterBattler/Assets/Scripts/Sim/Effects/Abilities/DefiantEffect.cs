using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Defiant: sharply raises Atk (+2) when a stat is lowered by an opposing Pokemon.</summary>
    public sealed class DefiantEffect : Effect
    {
        public override string EffectId => "defiant";
        public override string DisplayName => "Defiant";
        public override void OnAfterStatLowered(StatModifyEvent ev, Pokemon owner)
        {
            if (owner != ev.Owner) return;
            ev.Battle.BoostStat(owner, Stat.Atk, +2, source: owner);
        }
    }
}
