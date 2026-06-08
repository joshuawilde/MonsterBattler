using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Dragon's Maw: the owner's Dragon-type moves get a 1.5× base-power multiplier.</summary>
    public sealed class DragonsMawEffect : Effect
    {
        public override string EffectId => "dragonsmaw";
        public override string DisplayName => "Dragon's Maw";

        public override void OnBasePower(BasePowerEvent ev, Pokemon owner)
        {
            if (owner != ev.User) return;
            if (ev.Move?.Type != MonType.Dragon) return;
            ev.BasePower = ev.BasePower * 3 / 2;
        }
    }
}
