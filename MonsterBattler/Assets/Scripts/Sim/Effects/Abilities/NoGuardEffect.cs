using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>No Guard: moves used by or against the owner never miss.</summary>
    public sealed class NoGuardEffect : Effect
    {
        public override string EffectId => "noguard";
        public override string DisplayName => "No Guard";

        public override void OnModifyAccuracy(ModifyAccuracyEvent ev, Pokemon owner)
        {
            // Owner's own moves always hit, and moves targeting the owner always hit.
            if (owner == ev.User || owner == ev.Target)
                ev.Accuracy = 100;
        }
    }
}
