using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Protosynthesis: in harsh sunlight (or on consuming Booster Energy), boosts the holder's
    /// highest stat — ×1.3, or ×1.5 if that stat is Speed. See <see cref="Volatiles.ParadoxBoostVolatile"/>.
    /// </summary>
    public sealed class ProtosynthesisEffect : Effect
    {
        public override string EffectId => "protosynthesis";
        public override string DisplayName => "Protosynthesis";

        public override void OnSwitchIn(SwitchInEvent ev, Pokemon owner)
        {
            var w = ev.Battle.Field.Weather;
            if (w == Weather.Sun || w == Weather.HarshSun)
            {
                Volatiles.ParadoxBoostVolatile.Activate(owner, ev.Battle, "sun", DisplayName);
            }
            else if (owner.HasItem("boosterenergy"))
            {
                owner.Item = null;
                ev.Battle.Log.Raw($"|-enditem|{owner.Species?.Name ?? owner.Nickname}|Booster Energy");
                Volatiles.ParadoxBoostVolatile.Activate(owner, ev.Battle, "booster", DisplayName);
            }
        }
    }
}
