using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Rough Skin: attackers that make contact lose 1/8 of their max HP.</summary>
    public sealed class RoughSkinEffect : Effect
    {
        public override string EffectId => "roughskin";
        public override string DisplayName => "Rough Skin";

        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            if (ev.Move == null || !ev.Move.Contact) return;
            var attacker = ev.User;
            if (attacker == null || attacker.IsFainted) return;

            int dmg = System.Math.Max(1, attacker.MaxStats[(int)Stat.HP] / 8);
            ev.Battle.ApplyDamage(attacker, dmg, DamageSource.Other);
            ev.Battle.Log.Raw($"|-damage|{Ident(attacker)}|{attacker.CurrentHp}/{attacker.MaxStats[(int)Stat.HP]}|[from] ability: Rough Skin|[of] {Ident(owner)}");
        }

        static string Ident(Pokemon mon) => mon?.Nickname ?? mon?.Species?.Name ?? "?";
    }
}
