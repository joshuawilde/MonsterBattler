using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Destiny Bond: marks the user so a foe that KOs it also faints.</summary>
    public sealed class DestinyBondMoveEffect : Effect
    {
        public override string EffectId => "destinybondmove";
        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            if (ev.User == null || ev.User.IsFainted) return;
            ev.Battle.AddVolatile(ev.User, "destinybond", ev.User);
        }
    }
}
