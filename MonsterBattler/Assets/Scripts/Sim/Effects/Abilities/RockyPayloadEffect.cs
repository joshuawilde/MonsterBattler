using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Rocky Payload: ×1.5 base power on the owner's Rock-type moves.</summary>
    public sealed class RockyPayloadEffect : Effect
    {
        public override string EffectId => "rockypayload";
        public override string DisplayName => "Rocky Payload";

        public override void OnBasePower(BasePowerEvent ev, Pokemon owner)
        {
            if (owner != ev.User) return;
            if (ev.Move?.Type != MonType.Rock) return;
            ev.BasePower = ev.BasePower * 3 / 2;
        }
    }
}
