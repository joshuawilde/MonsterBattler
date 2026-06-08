using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>As One (Glastrier): raises Atk by 1 when the owner knocks out a foe (the Neigh half of As One).</summary>
    public sealed class AsOneGlastrierEffect : Effect
    {
        public override string EffectId => "asoneglastrier";
        public override string DisplayName => "As One (Glastrier)";
        public override void OnFaint(FaintEvent ev, Pokemon owner)
        {
            if (owner != ev.Source || owner.IsFainted) return;
            ev.Battle.BoostStat(owner, Stat.Atk, +1, source: owner);
        }
    }
}
