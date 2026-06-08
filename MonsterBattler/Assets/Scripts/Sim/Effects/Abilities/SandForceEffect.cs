using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Sand Force: in a Sandstorm, ×1.3 base power on the owner's Rock-, Ground-, and Steel-type moves.</summary>
    public sealed class SandForceEffect : Effect
    {
        public override string EffectId => "sandforce";
        public override string DisplayName => "Sand Force";

        public override void OnBasePower(BasePowerEvent ev, Pokemon owner)
        {
            if (owner != ev.User) return;
            if (ev.Battle.Field.Weather != Weather.Sandstorm) return;
            var type = ev.Move?.Type;
            if (type != MonType.Rock && type != MonType.Ground && type != MonType.Steel) return;
            ev.BasePower = ev.BasePower * 13 / 10;
        }
    }
}
