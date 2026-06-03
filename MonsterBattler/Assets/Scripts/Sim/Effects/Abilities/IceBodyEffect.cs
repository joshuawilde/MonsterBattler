using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class IceBodyEffect : Effect
    {
        public override string EffectId => "icebody";
        public override string DisplayName => "Ice Body";

        public override void OnResidual(ResidualEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || owner.IsFainted) return;
            if (ev.Battle.Field.Weather != Weather.Snow) return;
            int max = owner.MaxStats[(int)Stat.HP];
            int heal = System.Math.Min(max / 16, max - owner.CurrentHp);
            if (heal <= 0) return;
            owner.CurrentHp += heal;
            ev.Battle.Log.Raw($"|-heal|{owner.Species?.Name ?? owner.Nickname}|{owner.CurrentHp}/{max}|[from] ability: Ice Body");
        }
    }
}
