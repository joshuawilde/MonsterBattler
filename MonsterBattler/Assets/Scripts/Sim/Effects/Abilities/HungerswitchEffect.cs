using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Hunger Switch: Morpeko alternates Full Belly / Hangry form every turn.</summary>
    public sealed class HungerSwitchEffect : Effect
    {
        public override string EffectId => "hungerswitch";
        public override string DisplayName => "Hunger Switch";
        public override void OnResidual(ResidualEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || owner.IsFainted) return;
            if (owner.Species?.Id == "morpeko") ev.Battle.ChangeForm(owner, "morpekohangry");
            else if (owner.Species?.Id == "morpekohangry") ev.Battle.ChangeForm(owner, "morpeko");
        }
    }
}
