using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Cud Chew: re-eats the Berry it consumed at the end of the next turn (re-applies the berry effect once).</summary>
    public sealed class CudChewEffect : Effect
    {
        public override string EffectId => "cudchew";
        public override string DisplayName => "Cud Chew";
        public override void OnResidual(ResidualEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || owner.IsFainted) return;
            // Two-turn cycle via a counter tag: set the turn a berry was consumed, fire one turn later.
            if (owner.LostItem != null && owner.LostItem.IsBerry && owner.Item == null && !owner.Tags.Contains("cudchewed"))
            {
                if (!owner.Tags.Contains("cudchewpending")) { owner.Tags.Add("cudchewpending"); return; }
                owner.Tags.Remove("cudchewpending"); owner.Tags.Add("cudchewed");
                var berry = MonsterBattler.Sim.Effects.EffectRegistry.Get(owner.LostItem.EffectId ?? owner.LostItem.Id);
                berry?.OnResidual(ev, owner);        // re-trigger Sitrus/Lum/etc.
                berry?.OnDamagingHit(new HitEvent { Battle = ev.Battle, Target = owner, User = owner }, owner);
            }
        }
    }
}
