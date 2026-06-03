using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class PixilateEffect : Effect
    {
        public override string EffectId => "pixilate";
        public override string DisplayName => "Pixilate";

        public override void OnModifyType(ModifyTypeEvent ev, Pokemon owner)
        {
            if (owner != ev.User || ev.Move == null || ev.Type != MonType.Normal) return;
            ev.Type = MonType.Fairy;
            ev.BasePowerBonus += 20;
        }
    }
}
