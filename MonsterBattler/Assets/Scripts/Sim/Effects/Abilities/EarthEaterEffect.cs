using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Earth Eater: immune to Ground moves; heals 1/4 max HP instead.</summary>
    public sealed class EarthEaterEffect : Effect
    {
        public override string EffectId => "eartheater";
        public override string DisplayName => "Earth Eater";
        public override void OnTryHit(TryHitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move?.Type != MonType.Ground) return;
            ev.Blocked = true; ev.BlockReason = "Earth Eater";
            int max = owner.MaxStats[(int)Stat.HP];
            int heal = System.Math.Min(max / 4, max - owner.CurrentHp);
            if (heal > 0) { owner.CurrentHp += heal; ev.Battle.Log.Raw($"|-heal|{owner.Species?.Name ?? owner.Nickname}|{owner.CurrentHp}/{max}|[from] ability: Earth Eater"); }
        }
    }
}
