using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class StrongJawEffect : Effect
    {
        public override string EffectId => "strongjaw";
        public override string DisplayName => "Strong Jaw";

        public override void OnBasePower(BasePowerEvent ev, Pokemon owner)
        {
            if (owner != ev.User || ev.Move == null || !ev.Move.Bite) return;
            ev.BasePower = ev.BasePower * 3 / 2;
        }
    }
}
