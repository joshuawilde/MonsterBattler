using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class IronFistEffect : Effect
    {
        public override string EffectId => "ironfist";
        public override string DisplayName => "Iron Fist";

        public override void OnBasePower(BasePowerEvent ev, Pokemon owner)
        {
            if (owner != ev.User || ev.Move == null || !ev.Move.Punch) return;
            ev.BasePower = ev.BasePower * 12 / 10;
        }
    }
}
