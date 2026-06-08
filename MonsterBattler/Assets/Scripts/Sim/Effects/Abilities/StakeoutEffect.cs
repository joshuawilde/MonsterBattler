using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Stakeout: double damage against a target that switched in this turn.</summary>
    public sealed class StakeoutEffect : Effect
    {
        public override string EffectId => "stakeout";
        public override string DisplayName => "Stakeout";
        public override void OnBasePower(BasePowerEvent ev, Pokemon owner)
        {
            if (owner == ev.User && ev.Target != null && ev.Target.SwitchedInThisTurn)
                ev.BasePower = ev.BasePower * 2;
        }
    }
}
