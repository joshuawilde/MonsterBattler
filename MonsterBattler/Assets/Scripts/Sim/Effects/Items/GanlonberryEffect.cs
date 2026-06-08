using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Items
{
    /// <summary>Ganlon Berry: at 1/4 HP or less, raises Def by 1, then is consumed.</summary>
    public sealed class GanlonberryEffect : Effect
    {
        public override string EffectId => "ganlonberry";
        public override string DisplayName => "Ganlon Berry";
        public override void OnDamagingHit(HitEvent ev, Pokemon owner) { if (owner == ev.Target) Eat(ev.Battle, owner); }
        public override void OnResidual(ResidualEvent ev, Pokemon owner) { if (owner == ev.Target) Eat(ev.Battle, owner); }
        void Eat(Battle b, Pokemon owner)
        {
            if (b.BerriesSuppressed(owner)) return;
            if (owner.IsFainted || owner.CurrentHp * 4 > owner.MaxStats[(int)Stat.HP]) return;
            b.BoostStat(owner, Stat.Def, 1, owner);
            b.ConsumeItem(owner, "item: Ganlon Berry");
        }
    }
}
