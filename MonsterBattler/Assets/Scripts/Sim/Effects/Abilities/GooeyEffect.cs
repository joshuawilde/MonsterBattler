using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Gooey: lowers the Speed of attackers that make contact.</summary>
    public sealed class GooeyEffect : Effect
    {
        public override string EffectId => "gooey";
        public override string DisplayName => "Gooey";
        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move == null || !ev.Move.Contact) return;
            if (ev.User == null || ev.User.IsFainted) return;
            ev.Battle.BoostStat(ev.User, Stat.Spe, -1, source: owner);
        }
    }
}
