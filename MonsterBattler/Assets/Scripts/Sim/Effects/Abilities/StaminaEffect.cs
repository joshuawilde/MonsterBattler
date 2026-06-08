using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Stamina: raises Defense by 1 stage whenever the owner is hit by an attack.</summary>
    public sealed class StaminaEffect : Effect
    {
        public override string EffectId => "stamina";
        public override string DisplayName => "Stamina";

        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            if (owner.IsFainted) return;
            ev.Battle.BoostStat(owner, Stat.Def, 1, source: owner);
        }
    }
}
