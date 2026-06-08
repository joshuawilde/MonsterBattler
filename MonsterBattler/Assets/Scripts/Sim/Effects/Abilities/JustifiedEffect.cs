using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Justified: raises Atk by 1 stage when hit by a Dark-type move.</summary>
    public sealed class JustifiedEffect : Effect
    {
        public override string EffectId => "justified";
        public override string DisplayName => "Justified";

        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            if (ev.Move?.Type != MonType.Dark) return;
            ev.Battle.BoostStat(owner, Stat.Atk, 1, source: owner);
        }
    }
}
