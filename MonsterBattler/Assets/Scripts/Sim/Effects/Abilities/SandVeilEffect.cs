using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Sand Veil: in Sandstorm, opposing moves' accuracy ×0.8 (1/1.25 evasion boost).</summary>
    public sealed class SandVeilEffect : Effect
    {
        public override string EffectId => "sandveil";
        public override string DisplayName => "Sand Veil";

        public override void OnModifyAccuracy(ModifyAccuracyEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            if (ev.Battle.Field.Weather != Weather.Sandstorm) return;
            ev.Accuracy = ev.Accuracy * 4 / 5;
        }
    }
}
