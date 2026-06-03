using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class GalvanizeEffect : Effect
    {
        public override string EffectId => "galvanize";
        public override string DisplayName => "Galvanize";

        public override void OnModifyType(ModifyTypeEvent ev, Pokemon owner)
        {
            if (owner != ev.User || ev.Move == null || ev.Type != MonType.Normal) return;
            ev.Type = MonType.Electric;
            ev.BasePowerBonus += 20;
        }
    }
}
