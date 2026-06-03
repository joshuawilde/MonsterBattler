using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class AerilateEffect : Effect
    {
        public override string EffectId => "aerilate";
        public override string DisplayName => "Aerilate";

        public override void OnModifyType(ModifyTypeEvent ev, Pokemon owner)
        {
            if (owner != ev.User || ev.Move == null || ev.Type != MonType.Normal) return;
            ev.Type = MonType.Flying;
            ev.BasePowerBonus += 20;
        }
    }
}
