using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Mega Launcher: the owner's pulse and aura moves get a 1.5× base-power multiplier.
    /// (MoveData has no "pulse" flag, so the affected moves are matched by id.)
    /// </summary>
    public sealed class MegaLauncherEffect : Effect
    {
        public override string EffectId => "megalauncher";
        public override string DisplayName => "Mega Launcher";

        public override void OnBasePower(BasePowerEvent ev, Pokemon owner)
        {
            if (owner != ev.User) return;
            var id = ev.Move?.Id;
            if (id == null) return;
            switch (id)
            {
                case "aurasphere":
                case "darkpulse":
                case "dragonpulse":
                case "originpulse":
                case "terrainpulse":
                case "waterpulse":
                case "healpulse":
                    ev.BasePower = ev.BasePower * 3 / 2;
                    break;
            }
        }
    }
}
