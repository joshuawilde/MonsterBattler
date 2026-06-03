using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Statuses
{
    /// <summary>
    /// Badly Poisoned (Toxic): residual damage escalates each turn — turn N deals N/16 max HP.
    /// The counter lives on <see cref="Pokemon.ToxicCounter"/> and resets on switch out.
    /// </summary>
    public sealed class BadlyPoisonedStatus : Effect
    {
        public override string EffectId => "tox";
        public override string DisplayName => "Toxic";

        public override void OnResidual(ResidualEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || owner.IsFainted) return;
            owner.ToxicCounter++;
            int dmg = System.Math.Max(1, owner.MaxStats[(int)Stat.HP] * owner.ToxicCounter / 16);
            ev.Battle.ApplyDamage(owner, dmg);
            ev.Battle.Log.Raw($"|-damage|{owner.Species?.Name ?? owner.Nickname}|{owner.CurrentHp}/{owner.MaxStats[(int)Stat.HP]}|[from] tox");
        }
    }
}
