using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Statuses
{
    /// <summary>
    /// Paralysis: halves the owner's Speed (gen 7+ value, was ×1/4 prior). The 25%-fully-paralyzed
    /// chance lands when we add BeforeMove events — for now this is just the Speed cut.
    /// </summary>
    public sealed class ParalysisStatus : Effect
    {
        public override string EffectId => "par";
        public override string DisplayName => "Paralysis";

        public override void OnModifySpe(StatModifyEvent ev, Pokemon owner)
        {
            if (owner != ev.Owner) return;
            ev.Value /= 2;
        }
    }
}
