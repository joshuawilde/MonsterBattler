using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Unburden: doubles Speed once the holder has lost or consumed its item.</summary>
    public sealed class UnburdenEffect : Effect
    {
        public override string EffectId => "unburden";
        public override string DisplayName => "Unburden";
        public override void OnModifySpe(StatModifyEvent ev, Pokemon owner)
        {
            if (owner == ev.Owner && owner.ItemLost && owner.Item == null) ev.Value *= 2;
        }
    }
}
