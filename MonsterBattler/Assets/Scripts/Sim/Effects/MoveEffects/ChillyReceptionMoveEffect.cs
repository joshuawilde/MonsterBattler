using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Chilly Reception: sets Snow, then the user switches out (pivotsOut).</summary>
    public sealed class ChillyReceptionMoveEffect : Effect
    {
        public override string EffectId => "chillyreceptionmove";
        public override void OnHit(HitEvent ev, Pokemon owner) => ev.Battle.SetWeather(Weather.Snow, 5);
    }
}
