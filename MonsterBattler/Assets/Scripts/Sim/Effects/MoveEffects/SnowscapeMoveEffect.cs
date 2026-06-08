using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.MoveEffects
{
    public sealed class SnowscapeMoveEffect : Effect
    {
        public override string EffectId => "snowscapemove";
        public override void OnHit(HitEvent ev, Pokemon owner) => ev.Battle.SetWeather(Weather.Snow, 5);
    }
}
