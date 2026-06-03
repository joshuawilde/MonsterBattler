using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Speed Boost: at end of every turn, raise owner's Speed by 1 stage.
    /// </summary>
    public sealed class SpeedBoostEffect : Effect
    {
        public override string EffectId => "speedboost";
        public override string DisplayName => "Speed Boost";

        public override void OnResidual(ResidualEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || owner.IsFainted) return;
            ev.Battle.BoostStat(owner, Stat.Spe, +1);
        }
    }
}
