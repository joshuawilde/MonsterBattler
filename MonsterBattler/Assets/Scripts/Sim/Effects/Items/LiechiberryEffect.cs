using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Items
{
    /// <summary>Liechi Berry: at 1/4 HP or less, raises Atk by 1, then is consumed.</summary>
    public sealed class LiechiberryEffect : Effect
    {
        public override string EffectId => "liechiberry";
        public override string DisplayName => "Liechi Berry";
        public override void OnDamagingHit(HitEvent ev, Pokemon owner) { if (owner == ev.Target) Eat(ev.Battle, owner); }
        public override void OnResidual(ResidualEvent ev, Pokemon owner) { if (owner == ev.Target) Eat(ev.Battle, owner); }
        void Eat(Battle b, Pokemon owner)
        {
            if (b.BerriesSuppressed(owner)) return;
            if (owner.IsFainted || owner.CurrentHp * 4 > owner.MaxStats[(int)Stat.HP]) return;
            b.BoostStat(owner, Stat.Atk, 1, owner);
            b.ConsumeItem(owner, "item: Liechi Berry");
        }
    }
}
