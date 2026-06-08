using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Liquid Voice: the owner's sound-based moves become Water-type.</summary>
    public sealed class LiquidVoiceEffect : Effect
    {
        public override string EffectId => "liquidvoice";
        public override string DisplayName => "Liquid Voice";

        public override void OnModifyType(ModifyTypeEvent ev, Pokemon owner)
        {
            if (owner != ev.User || ev.Move == null || !ev.Move.Sound) return;
            ev.Type = MonType.Water;
        }
    }
}
