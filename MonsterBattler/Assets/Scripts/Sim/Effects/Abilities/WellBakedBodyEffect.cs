using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Well-Baked Body: immune to Fire moves; raises Def by 2 instead.</summary>
    public sealed class WellBakedBodyEffect : Effect
    {
        public override string EffectId => "wellbakedbody";
        public override string DisplayName => "Well-Baked Body";
        public override void OnTryHit(TryHitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move?.Type != MonType.Fire) return;
            ev.Blocked = true; ev.BlockReason = "Well-Baked Body";
            ev.Battle.BoostStat(owner, Stat.Def, +2, source: owner);
        }
    }
}
