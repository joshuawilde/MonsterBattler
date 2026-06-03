using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Items
{
    /// <summary>
    /// Life Orb: ×1.3 damage on attacking moves, owner takes 1/10 max HP recoil per damaging hit.
    /// </summary>
    public sealed class LifeOrbEffect : Effect
    {
        public override string EffectId => "lifeorb";
        public override string DisplayName => "Life Orb";

        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (owner != ev.User) return;
            if (ev.Move == null || ev.Move.Category == MoveCategory.Status) return;
            ev.Damage = ev.Damage * 13 / 10;
        }

        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.User || owner.IsFainted) return;
            if (ev.Move == null || ev.Move.Category == MoveCategory.Status) return;
            int max = owner.MaxStats[(int)Stat.HP];
            int recoil = System.Math.Max(1, max / 10);
            ev.Battle.ApplyDamage(owner, recoil, DamageSource.LifeOrb);
            ev.Battle.Log.Raw($"|-damage|{owner.Species?.Name ?? owner.Nickname}|{owner.CurrentHp}/{max}|[from] item: Life Orb");
        }
    }
}
