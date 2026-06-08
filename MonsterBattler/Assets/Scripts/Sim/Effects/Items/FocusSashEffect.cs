using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Items
{
    /// <summary>Focus Sash: at full HP, survive a would-be KO with 1 HP, then is consumed.</summary>
    public sealed class FocusSashEffect : Effect
    {
        public override string EffectId => "focussash";
        public override string DisplayName => "Focus Sash";
        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            int max = owner.MaxStats[(int)Stat.HP];
            if (owner.CurrentHp != max || ev.Damage < max) return;
            ev.Damage = max - 1;
            ev.Battle.Log.Raw($"|-activate|{owner.Species?.Name ?? owner.Nickname}|item: Focus Sash");
            ev.Battle.ConsumeItem(owner, "item: Focus Sash");
        }
    }
}
