using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Tough Claws: ×1.3 base power on the user's contact moves.</summary>
    public sealed class ToughClawsEffect : Effect
    {
        public override string EffectId => "toughclaws";
        public override string DisplayName => "Tough Claws";

        public override void OnBasePower(BasePowerEvent ev, Pokemon owner)
        {
            if (owner != ev.User || ev.Move == null || !ev.Move.Contact) return;
            ev.BasePower = ev.BasePower * 13 / 10;
        }
    }
}
