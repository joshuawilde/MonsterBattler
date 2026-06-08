using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Torrent: while owner HP ≤ 1/3, its Water moves get 1.5× base power.</summary>
    public sealed class TorrentEffect : Effect
    {
        public override string EffectId => "torrent";
        public override string DisplayName => "Torrent";
        public override void OnBasePower(BasePowerEvent ev, Pokemon owner)
        {
            if (owner != ev.User || ev.Move?.Type != MonType.Water) return;
            if (owner.CurrentHp * 3 > owner.MaxStats[(int)Stat.HP]) return;
            ev.BasePower = ev.BasePower * 3 / 2;
        }
    }
}
