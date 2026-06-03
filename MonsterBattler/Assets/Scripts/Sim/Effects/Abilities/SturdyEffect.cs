using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Sturdy: if owner is at full HP and an attack would otherwise KO them, the damage is
    /// reduced to leave them at 1 HP. Once-per-switch-in protection.
    /// </summary>
    public sealed class SturdyEffect : Effect
    {
        public override string EffectId => "sturdy";
        public override string DisplayName => "Sturdy";

        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            int max = owner.MaxStats[(int)Stat.HP];
            if (owner.CurrentHp != max) return;
            if (ev.Damage < max) return;
            ev.Damage = max - 1;
            ev.Battle.Log.Raw($"|-activate|{owner.Species?.Name ?? owner.Nickname}|ability: Sturdy");
        }
    }
}
