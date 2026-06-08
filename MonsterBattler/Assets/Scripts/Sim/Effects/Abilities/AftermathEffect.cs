using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Aftermath: when KO'd by a contact move, the attacker loses 1/4 of its max HP.</summary>
    public sealed class AftermathEffect : Effect
    {
        public override string EffectId => "aftermath";
        public override string DisplayName => "Aftermath";
        public override void OnFaint(FaintEvent ev, Pokemon owner)
        {
            if (owner != ev.Pokemon) return;                 // only when WE faint
            if (!owner.LastDamageWasContact) return;          // only from a contact move
            var atk = ev.Source;
            if (atk == null || atk.IsFainted) return;
            int dmg = System.Math.Max(1, atk.MaxStats[(int)Stat.HP] / 4);
            ev.Battle.ApplyDamage(atk, dmg, DamageSource.Other);
            ev.Battle.Log.Raw($"|-damage|{atk.Species?.Name ?? atk.Nickname}|{atk.CurrentHp}/{atk.MaxStats[(int)Stat.HP]}|[from] ability: Aftermath");
        }
    }
}
