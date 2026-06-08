using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Magician: steals the target's held item when the owner damages it (if the owner has none).</summary>
    public sealed class MagicianEffect : Effect
    {
        public override string EffectId => "magician";
        public override string DisplayName => "Magician";
        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.User) return;
            var t = ev.Target;
            if (t == null || t.Item == null || owner.Item != null) return;
            if (t.AbilityEffect is StickyHoldEffect) return;
            owner.Item = t.Item; owner.ItemEffect = t.ItemEffect;
            t.Item = null; t.ItemEffect = null; t.ItemLost = true;
            ev.Battle.Log.Raw($"|-item|{owner.Species?.Name ?? owner.Nickname}|{owner.Item.Name}|[from] ability: Magician");
        }
    }
}
