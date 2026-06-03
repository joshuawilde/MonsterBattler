using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class SwiftSwimEffect : Effect
    {
        public override string EffectId => "swiftswim";
        public override string DisplayName => "Swift Swim";

        public override void OnModifySpe(StatModifyEvent ev, Pokemon owner)
        {
            if (owner != ev.Owner) return;
            if (ev.Battle.Field.Weather != Weather.Rain && ev.Battle.Field.Weather != Weather.HeavyRain) return;
            ev.Value *= 2;
        }
    }
}
