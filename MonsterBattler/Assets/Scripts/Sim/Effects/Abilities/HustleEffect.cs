using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Hustle: Attack ×1.5, but physical move accuracy ×0.8.</summary>
    public sealed class HustleEffect : Effect
    {
        public override string EffectId => "hustle";
        public override string DisplayName => "Hustle";

        public override void OnModifyAtk(StatModifyEvent ev, Pokemon owner)
        {
            if (owner == ev.Owner) ev.Value = ev.Value * 3 / 2;
        }

        public override void OnModifyAccuracy(ModifyAccuracyEvent ev, Pokemon owner)
        {
            if (owner != ev.User || ev.Move == null) return;
            if (ev.Move.Category != MoveCategory.Physical) return;
            ev.Accuracy = ev.Accuracy * 4 / 5;
        }
    }
}
