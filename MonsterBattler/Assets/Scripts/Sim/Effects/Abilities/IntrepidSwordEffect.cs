using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Intrepid Sword: raises Atk by 1 on switch-in (once per battle).</summary>
    public sealed class IntrepidSwordEffect : Effect
    {
        public override string EffectId => "intrepidsword";
        public override string DisplayName => "Intrepid Sword";
        public override void OnSwitchIn(SwitchInEvent ev, Pokemon owner)
        {
            if (owner != ev.Pokemon) return;
            if (!owner.Tags.Add("intrepidsword_used")) return; // once per battle
            ev.Battle.BoostStat(owner, Stat.Atk, +1, source: owner);
        }
    }
}
