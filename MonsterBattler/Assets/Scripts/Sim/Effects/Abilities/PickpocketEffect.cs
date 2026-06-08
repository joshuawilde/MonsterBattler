using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Pickpocket: steals the attacker's item when hit by a contact move (if the owner has none).</summary>
    public sealed class PickpocketEffect : Effect
    {
        public override string EffectId => "pickpocket";
        public override string DisplayName => "Pickpocket";
        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move == null || !ev.Move.Contact) return;
            var atk = ev.User;
            if (atk == null || atk.Item == null || owner.Item != null) return;
            if (atk.AbilityEffect is StickyHoldEffect) return;
            owner.Item = atk.Item; owner.ItemEffect = atk.ItemEffect;
            atk.Item = null; atk.ItemEffect = null; atk.ItemLost = true;
            ev.Battle.Log.Raw($"|-item|{owner.Species?.Name ?? owner.Nickname}|{owner.Item.Name}|[from] ability: Pickpocket");
        }
    }
}
