using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.MoveEffects
{
    public sealed class SunnyDayMoveEffect : Effect
    {
        public override string EffectId => "sunnydaymove";
        public override void OnHit(HitEvent ev, Pokemon owner) => ev.Battle.SetWeather(Weather.Sun, 5);
    }
}
