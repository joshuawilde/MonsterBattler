using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Tera Shift: Terapagos changes to its Terastal Form on switch-in.</summary>
    public sealed class TerashiftEffect : Effect
    {
        public override string EffectId => "terashift";
        public override string DisplayName => "Tera Shift";
        public override void OnSwitchIn(SwitchInEvent ev, Pokemon owner)
        {
            if (owner == ev.Pokemon && owner.Species?.Id == "terapagos") ev.Battle.ChangeForm(owner, "terapagosterastal");
        }
    }
}
