using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Solar Power: while Sun is up, special attacks get ×1.5 SpA and the owner loses 1/8
    /// max HP at end of every turn.
    /// </summary>
    public sealed class SolarPowerEffect : Effect
    {
        public override string EffectId => "solarpower";
        public override string DisplayName => "Solar Power";

        public override void OnModifySpA(StatModifyEvent ev, Pokemon owner)
        {
            if (owner != ev.Owner) return;
            if (ev.Battle.Field.Weather != Weather.Sun && ev.Battle.Field.Weather != Weather.HarshSun) return;
            ev.Value = ev.Value * 3 / 2;
        }

        public override void OnResidual(ResidualEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || owner.IsFainted) return;
            if (ev.Battle.Field.Weather != Weather.Sun && ev.Battle.Field.Weather != Weather.HarshSun) return;
            int dmg = System.Math.Max(1, owner.MaxStats[(int)Stat.HP] / 8);
            ev.Battle.ApplyDamage(owner, dmg);
            ev.Battle.Log.Raw($"|-damage|{owner.Species?.Name ?? owner.Nickname}|{owner.CurrentHp}/{owner.MaxStats[(int)Stat.HP]}|[from] ability: Solar Power");
        }
    }
}
