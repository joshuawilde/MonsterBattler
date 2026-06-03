using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class SnowCloakEffect : Effect
    {
        public override string EffectId => "snowcloak";
        public override string DisplayName => "Snow Cloak";

        public override void OnModifyAccuracy(ModifyAccuracyEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            if (ev.Battle.Field.Weather != Weather.Snow) return;
            ev.Accuracy = ev.Accuracy * 4 / 5;
        }
    }
}
