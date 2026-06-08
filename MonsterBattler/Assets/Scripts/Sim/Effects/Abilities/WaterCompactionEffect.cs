using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Water Compaction: raises the owner's Defense by 2 stages when hit by a Water-type move.
    /// </summary>
    public sealed class WaterCompactionEffect : Effect
    {
        public override string EffectId => "watercompaction";
        public override string DisplayName => "Water Compaction";

        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            if (ev.Move?.Type != MonType.Water) return;
            ev.Battle.BoostStat(owner, Stat.Def, 2, source: owner);
        }
    }
}
