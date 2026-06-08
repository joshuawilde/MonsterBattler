using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Electromorphosis: becomes charged (next Electric move doubled) whenever it is hit.</summary>
    public sealed class ElectromorphosisEffect : Effect
    {
        public override string EffectId => "electromorphosis";
        public override string DisplayName => "Electromorphosis";
        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner == ev.Target && !owner.IsFainted) ev.Battle.AddVolatile(owner, "charge", owner);
        }
    }
}
