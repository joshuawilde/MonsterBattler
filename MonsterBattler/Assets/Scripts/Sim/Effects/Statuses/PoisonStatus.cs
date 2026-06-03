using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Statuses
{
    /// <summary>Regular poison: 1/8 max HP at end of turn, fixed.</summary>
    public sealed class PoisonStatus : Effect
    {
        public override string EffectId => "psn";
        public override string DisplayName => "Poison";

        public override void OnResidual(ResidualEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || owner.IsFainted) return;
            int dmg = System.Math.Max(1, owner.MaxStats[(int)Stat.HP] / 8);
            ev.Battle.ApplyDamage(owner, dmg, DamageSource.Poison);
            ev.Battle.Log.Raw($"|-damage|{owner.Species?.Name ?? owner.Nickname}|{owner.CurrentHp}/{owner.MaxStats[(int)Stat.HP]}|[from] psn");
        }
    }
}
