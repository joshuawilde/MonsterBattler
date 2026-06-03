using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class RainDishEffect : Effect
    {
        public override string EffectId => "raindish";
        public override string DisplayName => "Rain Dish";

        public override void OnResidual(ResidualEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || owner.IsFainted) return;
            if (ev.Battle.Field.Weather != Weather.Rain && ev.Battle.Field.Weather != Weather.HeavyRain) return;
            int max = owner.MaxStats[(int)Stat.HP];
            int heal = System.Math.Min(max / 16, max - owner.CurrentHp);
            if (heal <= 0) return;
            owner.CurrentHp += heal;
            ev.Battle.Log.Raw($"|-heal|{owner.Species?.Name ?? owner.Nickname}|{owner.CurrentHp}/{max}|[from] ability: Rain Dish");
        }
    }
}
