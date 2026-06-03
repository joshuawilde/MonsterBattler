using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Super Fang: deals damage equal to half the target's current HP (minimum 1).</summary>
    public sealed class SuperFangMoveEffect : Effect
    {
        public override string EffectId => "superfangmove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var t = ev.Target;
            if (t == null || t.IsFainted) return;
            int dmg = System.Math.Max(1, t.CurrentHp / 2);
            ev.Battle.ApplyDamage(t, dmg);
            ev.Battle.Log.Damage(t, dmg);
        }
    }
}
