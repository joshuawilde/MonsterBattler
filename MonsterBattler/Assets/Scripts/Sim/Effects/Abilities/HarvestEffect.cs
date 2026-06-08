using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Harvest: at end of turn, may restore a consumed Berry (always in sun, else 50%).</summary>
    public sealed class HarvestEffect : Effect
    {
        public override string EffectId => "harvest";
        public override string DisplayName => "Harvest";
        public override void OnResidual(ResidualEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || owner.IsFainted) return;
            if (owner.Item != null || owner.LostItem == null || !owner.LostItem.IsBerry) return;
            bool sun = ev.Battle.ActiveWeather() == Weather.Sun || ev.Battle.ActiveWeather() == Weather.HarshSun;
            if (!sun && !ev.Battle.Prng.Chance(1, 2)) return;
            owner.Item = owner.LostItem; owner.ItemEffect = MonsterBattler.Sim.Effects.EffectRegistry.Get(owner.Item.EffectId ?? owner.Item.Id);
            owner.ItemLost = false;
            ev.Battle.Log.Raw($"|-item|{owner.Species?.Name ?? owner.Nickname}|{owner.Item.Name}|[from] ability: Harvest");
        }
    }
}
