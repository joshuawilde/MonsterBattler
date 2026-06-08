using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Weak Armor: when hit by a physical move, the owner's Defense drops by 1 stage and its
    /// Speed rises by 2 stages.
    /// </summary>
    public sealed class WeakArmorEffect : Effect
    {
        public override string EffectId => "weakarmor";
        public override string DisplayName => "Weak Armor";

        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            if (ev.Move == null || ev.Move.Category != MoveCategory.Physical) return;
            ev.Battle.BoostStat(owner, Stat.Def, -1, source: owner);
            ev.Battle.BoostStat(owner, Stat.Spe, 2, source: owner);
        }
    }
}
