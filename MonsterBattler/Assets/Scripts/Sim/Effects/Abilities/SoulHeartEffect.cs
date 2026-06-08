using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Soul-Heart: raises Sp. Atk by 1 each time any Pokemon faints.</summary>
    public sealed class SoulHeartEffect : Effect
    {
        public override string EffectId => "soulheart";
        public override string DisplayName => "Soul-Heart";
        public override void OnFaint(FaintEvent ev, Pokemon owner)
        {
            if (owner == ev.Pokemon || owner.IsFainted) return; // any OTHER mon fainting
            ev.Battle.BoostStat(owner, Stat.SpA, +1, source: owner);
        }
    }
}
