using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Rattled: raises Speed by 1 stage when hit by a Dark-, Bug-, or Ghost-type move.</summary>
    public sealed class RattledEffect : Effect
    {
        public override string EffectId => "rattled";
        public override string DisplayName => "Rattled";

        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || owner.IsFainted) return;
            var type = ev.Move?.Type;
            if (type != MonType.Dark && type != MonType.Bug && type != MonType.Ghost) return;
            ev.Battle.BoostStat(owner, Stat.Spe, 1);
        }
    }
}
