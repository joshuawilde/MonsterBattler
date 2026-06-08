using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Swarm: while owner HP ≤ 1/3, its Bug moves get 1.5× base power.</summary>
    public sealed class SwarmEffect : Effect
    {
        public override string EffectId => "swarm";
        public override string DisplayName => "Swarm";
        public override void OnBasePower(BasePowerEvent ev, Pokemon owner)
        {
            if (owner != ev.User || ev.Move?.Type != MonType.Bug) return;
            if (owner.CurrentHp * 3 > owner.MaxStats[(int)Stat.HP]) return;
            ev.BasePower = ev.BasePower * 3 / 2;
        }
    }
}
