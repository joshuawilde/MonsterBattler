using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Water Bubble: owner's Water-type moves get a 2x base-power boost, Fire damage taken
    /// is halved, and the owner cannot be burned.
    /// </summary>
    public sealed class WaterBubbleEffect : Effect
    {
        public override string EffectId => "waterbubble";
        public override string DisplayName => "Water Bubble";

        public override void OnBasePower(BasePowerEvent ev, Pokemon owner)
        {
            if (owner != ev.User) return;
            if (ev.Move?.Type != MonType.Water) return;
            ev.BasePower *= 2;
        }

        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move == null) return;
            if (ev.Move.Type != MonType.Fire) return;
            ev.Damage /= 2;
        }

        public override void OnTryStatus(TryStatusEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            if (ev.Status != StatusCondition.Burn) return;
            ev.Blocked = true;
            ev.BlockReason = "Water Bubble";
        }
    }
}
