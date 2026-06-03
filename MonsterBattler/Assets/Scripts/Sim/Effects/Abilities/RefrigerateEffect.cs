using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class RefrigerateEffect : Effect
    {
        public override string EffectId => "refrigerate";
        public override string DisplayName => "Refrigerate";

        public override void OnModifyType(ModifyTypeEvent ev, Pokemon owner)
        {
            if (owner != ev.User || ev.Move == null || ev.Type != MonType.Normal) return;
            ev.Type = MonType.Ice;
            ev.BasePowerBonus += 20;
        }
    }
}
