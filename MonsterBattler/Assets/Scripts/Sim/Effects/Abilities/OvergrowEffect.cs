using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Overgrow: while owner HP ≤ 1/3, its Grass moves get 1.5× base power.</summary>
    public sealed class OvergrowEffect : Effect
    {
        public override string EffectId => "overgrow";
        public override string DisplayName => "Overgrow";
        public override void OnBasePower(BasePowerEvent ev, Pokemon owner)
        {
            if (owner != ev.User || ev.Move?.Type != MonType.Grass) return;
            if (owner.CurrentHp * 3 > owner.MaxStats[(int)Stat.HP]) return;
            ev.BasePower = ev.BasePower * 3 / 2;
        }
    }
}
