using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class VoltAbsorbEffect : Effect
    {
        public override string EffectId => "voltabsorb";
        public override string DisplayName => "Volt Absorb";

        public override void OnTryHit(TryHitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move?.Type != MonType.Electric) return;
            ev.Blocked = true;
            ev.BlockReason = "Volt Absorb";
            HealQuarter(owner, ev.Battle);
        }

        static void HealQuarter(Pokemon owner, Battle battle)
        {
            int max = owner.MaxStats[(int)Stat.HP];
            int heal = System.Math.Min(max / 4, max - owner.CurrentHp);
            if (heal <= 0) return;
            owner.CurrentHp += heal;
            battle.Log.Raw($"|-heal|{owner.Species?.Name ?? owner.Nickname}|{owner.CurrentHp}/{max}|[from] ability: Volt Absorb");
        }
    }
}
