using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class SandRushEffect : Effect
    {
        public override string EffectId => "sandrush";
        public override string DisplayName => "Sand Rush";

        public override void OnModifySpe(StatModifyEvent ev, Pokemon owner)
        {
            if (owner != ev.Owner) return;
            if (ev.Battle.Field.Weather != Weather.Sandstorm) return;
            ev.Value *= 2;
        }
    }
}
