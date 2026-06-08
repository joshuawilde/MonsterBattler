using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Triage: healing moves (drain or recovery) gain +3 priority.</summary>
    public sealed class TriageEffect : Effect
    {
        public override string EffectId => "triage";
        public override string DisplayName => "Triage";
        public override void OnModifyPriority(ModifyPriorityEvent ev, Pokemon owner)
        {
            if (owner != ev.User || ev.Move == null) return;
            bool heals = ev.Move.DrainNum > 0 ||
                ev.Move.EffectId == "recovermove" || ev.Move.EffectId == "synthesismove" || ev.Move.EffectId == "restmove";
            if (heals) ev.Priority += 3;
        }
    }
}
