using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Chilling Neigh: raises Atk by 1 when the owner knocks out a foe.</summary>
    public sealed class ChillingNeighEffect : Effect
    {
        public override string EffectId => "chillingneigh";
        public override string DisplayName => "Chilling Neigh";
        public override void OnFaint(FaintEvent ev, Pokemon owner)
        {
            if (owner != ev.Source || owner.IsFainted) return; // owner landed the KO
            ev.Battle.BoostStat(owner, Stat.Atk, +1, source: owner);
        }
    }
}
