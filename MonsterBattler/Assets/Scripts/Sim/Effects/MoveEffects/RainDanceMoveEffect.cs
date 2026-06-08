using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.MoveEffects
{
    public sealed class RainDanceMoveEffect : Effect
    {
        public override string EffectId => "raindancemove";
        public override void OnHit(HitEvent ev, Pokemon owner) => ev.Battle.SetWeather(Weather.Rain, 5);
    }
}
