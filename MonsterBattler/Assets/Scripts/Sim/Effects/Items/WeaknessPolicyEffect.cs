using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Items
{
    /// <summary>Weakness Policy: sharply raises Atk and Sp. Atk when hit by a super-effective move, then is consumed.</summary>
    public sealed class WeaknessPolicyEffect : Effect
    {
        public override string EffectId => "weaknesspolicy";
        public override string DisplayName => "Weakness Policy";
        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move == null || ev.Move.Category == MoveCategory.Status) return;
            var (t1, t2) = owner.CurrentTypes();
            if (TypeChart.Effectiveness(ev.Move.Type, t1, t2) <= 1f) return;
            ev.Battle.BoostStat(owner, Stat.Atk, 2, owner);
            ev.Battle.BoostStat(owner, Stat.SpA, 2, owner);
            ev.Battle.ConsumeItem(owner, "item: Weakness Policy");
        }
    }
}
