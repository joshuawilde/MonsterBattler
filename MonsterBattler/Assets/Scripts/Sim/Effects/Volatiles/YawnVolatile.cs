using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Volatiles
{
    /// <summary>Yawn: counts down at end of turn; when it expires the owner falls asleep.</summary>
    public sealed class YawnVolatile : Effect
    {
        public override string EffectId => "yawn";
        public override string DisplayName => "Yawn";

        public override void OnResidual(ResidualEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || owner.IsFainted) return;
            var slot = owner.GetVolatile("yawn");
            if (slot == null) return;
            slot.Turns--;
            if (slot.Turns <= 0)
            {
                ev.Battle.RemoveVolatile(owner, "yawn");
                if (owner.Status == StatusCondition.None)
                    ev.Battle.ApplyStatus(owner, StatusCondition.Sleep);
            }
        }
    }
}
