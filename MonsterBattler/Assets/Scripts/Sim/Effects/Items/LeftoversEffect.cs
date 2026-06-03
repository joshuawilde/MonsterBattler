using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Items
{
    /// <summary>Leftovers: heals 1/16 max HP at end of every turn.</summary>
    public sealed class LeftoversEffect : Effect
    {
        public override string EffectId => "leftovers";
        public override string DisplayName => "Leftovers";

        public override void OnResidual(ResidualEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || owner.IsFainted) return;
            int max = owner.MaxStats[(int)Stat.HP];
            if (owner.CurrentHp >= max) return;
            int heal = System.Math.Max(1, max / 16);
            owner.CurrentHp = System.Math.Min(max, owner.CurrentHp + heal);
            ev.Battle.Log.Raw($"|-heal|{owner.Species?.Name ?? owner.Nickname}|{owner.CurrentHp}/{max}|[from] item: Leftovers");
        }
    }
}
