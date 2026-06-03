using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Statuses
{
    /// <summary>
    /// Burn: halves the burned Pokemon's physical Atk; deals 1/16 max HP at end of turn.
    /// </summary>
    public sealed class BurnStatus : Effect
    {
        public override string EffectId => "brn";
        public override string DisplayName => "Burn";

        public override void OnModifyAtk(StatModifyEvent ev, Pokemon owner)
        {
            if (owner != ev.Owner) return;
            if (ev.ContextMove == null || ev.ContextMove.Category != MoveCategory.Physical) return;
            ev.Value /= 2;
        }

        public override void OnResidual(ResidualEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            int dmg = System.Math.Max(1, owner.MaxStats[(int)Stat.HP] / 16);
            ev.Battle.ApplyDamage(owner, dmg, DamageSource.Burn);
            ev.Battle.Log.Raw($"|-damage|{owner.Nickname ?? owner.Species?.Name}|{owner.CurrentHp}/{owner.MaxStats[(int)Stat.HP]}|[from] brn");
        }
    }
}
