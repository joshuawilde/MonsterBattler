using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Battle Bond: Greninja becomes its Bond (Ash) form after knocking out a foe.</summary>
    public sealed class BattlebondEffect : Effect
    {
        public override string EffectId => "battlebond";
        public override string DisplayName => "Battle Bond";
        public override void OnFaint(FaintEvent ev, Pokemon owner)
        {
            if (owner == ev.Source && !owner.IsFainted && owner.Species?.Id == "greninja")
                ev.Battle.ChangeForm(owner, "greninjabond");
        }
    }
}
