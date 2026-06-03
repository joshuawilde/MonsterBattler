using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Volatiles
{
    /// <summary>
    /// Bind / Wrap / Whirlpool / Fire Spin / Magma Storm: 4-5 turns of 1/8 max-HP residual damage
    /// and the target can't voluntarily switch while it's up. <see cref="VolatileSlot.Source"/>
    /// holds the trapper.
    /// </summary>
    public sealed class PartiallyTrappedVolatile : Effect
    {
        public override string EffectId => "partiallytrapped";
        public override string DisplayName => "Partially Trapped";

        public override void OnResidual(ResidualEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || owner.IsFainted) return;
            var slot = owner.GetVolatile("partiallytrapped");
            if (slot == null) return;
            // Trapper fainted or switched out — release.
            if (slot.Source == null || slot.Source.IsFainted || !slot.Source.IsActive)
            {
                ev.Battle.RemoveVolatile(owner, "partiallytrapped");
                return;
            }
            if (slot.Turns <= 0)
            {
                ev.Battle.RemoveVolatile(owner, "partiallytrapped");
                return;
            }
            slot.Turns--;
            int dmg = System.Math.Max(1, owner.MaxStats[(int)Stat.HP] / 8);
            ev.Battle.ApplyDamage(owner, dmg, DamageSource.TrappingMove);
            ev.Battle.Log.Raw($"|-damage|{owner.Species?.Name ?? owner.Nickname}|{owner.CurrentHp}/{owner.MaxStats[(int)Stat.HP]}|[from] partiallytrapped");
        }
    }
}
