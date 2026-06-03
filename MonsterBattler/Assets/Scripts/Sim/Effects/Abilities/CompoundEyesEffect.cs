using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Compound Eyes: user's move accuracy ×1.3.</summary>
    public sealed class CompoundEyesEffect : Effect
    {
        public override string EffectId => "compoundeyes";
        public override string DisplayName => "Compound Eyes";

        public override void OnModifyAccuracy(ModifyAccuracyEvent ev, Pokemon owner)
        {
            if (owner != ev.User) return;
            ev.Accuracy = ev.Accuracy * 13 / 10;
        }
    }
}
