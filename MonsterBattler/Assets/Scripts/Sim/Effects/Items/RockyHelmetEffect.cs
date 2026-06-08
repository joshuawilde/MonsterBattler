using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Items
{
    /// <summary>Rocky Helmet: attackers that make contact lose 1/6 of their max HP.</summary>
    public sealed class RockyHelmetEffect : Effect
    {
        public override string EffectId => "rockyhelmet";
        public override string DisplayName => "Rocky Helmet";
        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move == null || !ev.Move.Contact) return;
            var atk = ev.User;
            if (atk == null || atk.IsFainted) return;
            ev.Battle.ApplyDamage(atk, System.Math.Max(1, atk.MaxStats[(int)Stat.HP] / 6), DamageSource.Other);
            ev.Battle.Log.Raw($"|-damage|{atk.Species?.Name ?? atk.Nickname}|{atk.CurrentHp}/{atk.MaxStats[(int)Stat.HP]}|[from] item: Rocky Helmet");
        }
    }
}
