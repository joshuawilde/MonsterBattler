using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Grim Neigh: raises SpA by 1 when the owner knocks out a foe.</summary>
    public sealed class GrimNeighEffect : Effect
    {
        public override string EffectId => "grimneigh";
        public override string DisplayName => "Grim Neigh";
        public override void OnFaint(FaintEvent ev, Pokemon owner)
        {
            if (owner != ev.Source || owner.IsFainted) return; // owner landed the KO
            ev.Battle.BoostStat(owner, Stat.SpA, +1, source: owner);
        }
    }
}
