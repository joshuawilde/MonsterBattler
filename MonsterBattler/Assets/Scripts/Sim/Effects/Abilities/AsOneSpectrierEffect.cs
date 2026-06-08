using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>As One (Spectrier): raises SpA by 1 when the owner knocks out a foe (the Neigh half of As One).</summary>
    public sealed class AsOneSpectrierEffect : Effect
    {
        public override string EffectId => "asonespectrier";
        public override string DisplayName => "As One (Spectrier)";
        public override void OnFaint(FaintEvent ev, Pokemon owner)
        {
            if (owner != ev.Source || owner.IsFainted) return;
            ev.Battle.BoostStat(owner, Stat.SpA, +1, source: owner);
        }
    }
}
