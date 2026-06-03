using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class ChlorophyllEffect : Effect
    {
        public override string EffectId => "chlorophyll";
        public override string DisplayName => "Chlorophyll";

        public override void OnModifySpe(StatModifyEvent ev, Pokemon owner)
        {
            if (owner != ev.Owner) return;
            if (ev.Battle.Field.Weather != Weather.Sun && ev.Battle.Field.Weather != Weather.HarshSun) return;
            ev.Value *= 2;
        }
    }
}
