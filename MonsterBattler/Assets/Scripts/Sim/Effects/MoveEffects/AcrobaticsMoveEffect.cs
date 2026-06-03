using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Acrobatics: ×2 base power when the user has no held item.</summary>
    public sealed class AcrobaticsMoveEffect : Effect
    {
        public override string EffectId => "acrobaticsmove";

        public override void OnBasePower(BasePowerEvent ev, Pokemon owner)
        {
            if (ev.User != null && ev.User.Item == null) ev.BasePower *= 2;
        }
    }
}
