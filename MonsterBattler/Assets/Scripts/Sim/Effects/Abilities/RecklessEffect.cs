using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Reckless: ×1.2 base power on the user's recoil moves (also Jump Kick/High Jump Kick once those exist).</summary>
    public sealed class RecklessEffect : Effect
    {
        public override string EffectId => "reckless";
        public override string DisplayName => "Reckless";

        public override void OnBasePower(BasePowerEvent ev, Pokemon owner)
        {
            if (owner != ev.User || ev.Move == null) return;
            if (ev.Move.RecoilDen <= 0) return;
            ev.BasePower = ev.BasePower * 12 / 10;
        }
    }
}
