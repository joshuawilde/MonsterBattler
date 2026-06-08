using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Volatiles
{
    /// <summary>Charge: the owner's next Electric-type move has doubled base power, then this clears.</summary>
    public sealed class ChargeVolatile : Effect
    {
        public override string EffectId => "charge";
        public override string DisplayName => "Charge";
        public override void OnBasePower(BasePowerEvent ev, Pokemon owner)
        {
            if (owner != ev.User || ev.Move == null || ev.Move.Type != MonType.Electric) return;
            ev.BasePower *= 2;
            ev.Battle.RemoveVolatile(owner, "charge");
        }
    }
}
