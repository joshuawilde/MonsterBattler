using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class SharpnessEffect : Effect
    {
        public override string EffectId => "sharpness";
        public override string DisplayName => "Sharpness";

        public override void OnBasePower(BasePowerEvent ev, Pokemon owner)
        {
            if (owner != ev.User || ev.Move == null || !ev.Move.Slicing) return;
            ev.BasePower = ev.BasePower * 3 / 2;
        }
    }
}
