using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Transistor: boosts the power of the owner's Electric-type moves by 1.3×.</summary>
    public sealed class TransistorEffect : Effect
    {
        public override string EffectId => "transistor";
        public override string DisplayName => "Transistor";

        public override void OnBasePower(BasePowerEvent ev, Pokemon owner)
        {
            if (owner != ev.User) return;
            if (ev.Move?.Type != MonType.Electric) return;
            ev.BasePower = ev.BasePower * 13 / 10;
        }
    }
}
