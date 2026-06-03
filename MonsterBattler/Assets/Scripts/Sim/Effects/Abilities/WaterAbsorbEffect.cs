using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class WaterAbsorbEffect : Effect
    {
        public override string EffectId => "waterabsorb";
        public override string DisplayName => "Water Absorb";

        public override void OnTryHit(TryHitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move?.Type != MonType.Water) return;
            ev.Blocked = true;
            ev.BlockReason = "Water Absorb";
            int max = owner.MaxStats[(int)Stat.HP];
            int heal = System.Math.Min(max / 4, max - owner.CurrentHp);
            if (heal <= 0) return;
            owner.CurrentHp += heal;
            ev.Battle.Log.Raw($"|-heal|{owner.Species?.Name ?? owner.Nickname}|{owner.CurrentHp}/{max}|[from] ability: Water Absorb");
        }
    }
}
