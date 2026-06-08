using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Technician: the owner's moves with base power 60 or less get a 1.5× base-power multiplier.
    /// </summary>
    public sealed class TechnicianEffect : Effect
    {
        public override string EffectId => "technician";
        public override string DisplayName => "Technician";

        public override void OnBasePower(BasePowerEvent ev, Pokemon owner)
        {
            if (owner != ev.User) return;
            if (ev.BasePower > 60) return;
            ev.BasePower = ev.BasePower * 3 / 2;
        }
    }
}
