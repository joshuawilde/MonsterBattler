using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class SlushRushEffect : Effect
    {
        public override string EffectId => "slushrush";
        public override string DisplayName => "Slush Rush";

        public override void OnModifySpe(StatModifyEvent ev, Pokemon owner)
        {
            if (owner != ev.Owner) return;
            if (ev.Battle.Field.Weather != Weather.Snow) return;
            ev.Value *= 2;
        }
    }
}
