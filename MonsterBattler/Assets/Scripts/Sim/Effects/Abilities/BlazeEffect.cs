using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Blaze: while owner HP ≤ 1/3, owner's Fire-type moves get a 1.5× base-power multiplier.
    /// </summary>
    public sealed class BlazeEffect : Effect
    {
        public override string EffectId => "blaze";
        public override string DisplayName => "Blaze";

        public override void OnBasePower(BasePowerEvent ev, Pokemon owner)
        {
            if (owner != ev.User) return;
            if (ev.Move?.Type != MonType.Fire) return;
            int maxHp = owner.MaxStats[(int)Stat.HP];
            if (owner.CurrentHp * 3 > maxHp) return;
            ev.BasePower = ev.BasePower * 3 / 2;
        }
    }
}
