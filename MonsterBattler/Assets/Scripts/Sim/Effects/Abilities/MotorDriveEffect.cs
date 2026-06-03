using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class MotorDriveEffect : Effect
    {
        public override string EffectId => "motordrive";
        public override string DisplayName => "Motor Drive";

        public override void OnTryHit(TryHitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move?.Type != MonType.Electric) return;
            ev.Blocked = true;
            ev.BlockReason = "Motor Drive";
            ev.Battle.BoostStat(owner, Stat.Spe, +1);
        }
    }
}
