using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Wind Rider: immune to Flying moves; raises Atk by 1 instead.</summary>
    public sealed class WindRiderEffect : Effect
    {
        public override string EffectId => "windrider";
        public override string DisplayName => "Wind Rider";
        public override void OnTryHit(TryHitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move?.Type != MonType.Flying) return;
            ev.Blocked = true; ev.BlockReason = "Wind Rider";
            ev.Battle.BoostStat(owner, Stat.Atk, +1, source: owner);
        }
    }
}
