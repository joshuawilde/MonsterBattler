using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Items
{
    /// <summary>Apicot Berry: at 1/4 HP or less, raises SpD by 1, then is consumed.</summary>
    public sealed class ApicotberryEffect : Effect
    {
        public override string EffectId => "apicotberry";
        public override string DisplayName => "Apicot Berry";
        public override void OnDamagingHit(HitEvent ev, Pokemon owner) { if (owner == ev.Target) Eat(ev.Battle, owner); }
        public override void OnResidual(ResidualEvent ev, Pokemon owner) { if (owner == ev.Target) Eat(ev.Battle, owner); }
        void Eat(Battle b, Pokemon owner)
        {
            if (b.BerriesSuppressed(owner)) return;
            if (owner.IsFainted || owner.CurrentHp * 4 > owner.MaxStats[(int)Stat.HP]) return;
            b.BoostStat(owner, Stat.SpD, 1, owner);
            b.ConsumeItem(owner, "item: Apicot Berry");
        }
    }
}
