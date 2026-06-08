using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Dauntless Shield: raises Def by 1 on switch-in (once per battle).</summary>
    public sealed class DauntlessShieldEffect : Effect
    {
        public override string EffectId => "dauntlessshield";
        public override string DisplayName => "Dauntless Shield";
        public override void OnSwitchIn(SwitchInEvent ev, Pokemon owner)
        {
            if (owner != ev.Pokemon) return;
            if (!owner.Tags.Add("dauntlessshield_used")) return; // once per battle
            ev.Battle.BoostStat(owner, Stat.Def, +1, source: owner);
        }
    }
}
