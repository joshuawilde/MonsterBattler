using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Surge Surfer: doubles Speed on Electric Terrain.</summary>
    public sealed class SurgeSurferEffect : Effect
    {
        public override string EffectId => "surgesurfer";
        public override string DisplayName => "Surge Surfer";
        public override void OnModifySpe(StatModifyEvent ev, Pokemon owner)
        {
            if (owner == ev.Owner && ev.Battle.Field.Terrain == Terrain.Electric) ev.Value *= 2;
        }
    }
}
