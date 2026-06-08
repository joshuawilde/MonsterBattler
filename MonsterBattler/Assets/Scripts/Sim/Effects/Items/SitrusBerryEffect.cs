using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Items
{
    /// <summary>Sitrus Berry: at 1/2 HP or less, restores 1/4 max HP, then is consumed.</summary>
    public sealed class SitrusBerryEffect : Effect
    {
        public override string EffectId => "sitrusberry";
        public override string DisplayName => "Sitrus Berry";
        public override void OnDamagingHit(HitEvent ev, Pokemon owner) { if (owner == ev.Target) Eat(ev.Battle, owner); }
        public override void OnResidual(ResidualEvent ev, Pokemon owner) { if (owner == ev.Target) Eat(ev.Battle, owner); }
        void Eat(Battle b, Pokemon owner)
        {
            if (b.BerriesSuppressed(owner)) return;
            if (owner.IsFainted) return;
            int max = owner.MaxStats[(int)Stat.HP];
            if (owner.CurrentHp * 2 > max || owner.CurrentHp >= max) return;
            owner.CurrentHp = System.Math.Min(max, owner.CurrentHp + max / 4);
            b.Log.Raw($"|-heal|{owner.Species?.Name ?? owner.Nickname}|{owner.CurrentHp}/{max}|[from] item: Sitrus Berry");
            b.ConsumeItem(owner, "item: Sitrus Berry");
        }
    }
}
