using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Bad Dreams: at end of every turn, each opposing active Pokemon that is asleep loses
    /// 1/8 of its max HP.
    /// </summary>
    public sealed class BadDreamsEffect : Effect
    {
        public override string EffectId => "baddreams";
        public override string DisplayName => "Bad Dreams";

        public override void OnResidual(ResidualEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || owner.IsFainted) return;
            var opp = ev.Battle.OpposingSideOf(owner);
            if (opp == null) return;
            foreach (var foe in opp.ActiveSlots)
            {
                if (foe == null || foe.IsFainted) continue;
                if (foe.Status != StatusCondition.Sleep) continue;
                int dmg = System.Math.Max(1, foe.MaxStats[(int)Stat.HP] / 8);
                ev.Battle.ApplyDamage(foe, dmg);
                ev.Battle.Log.Raw($"|-damage|{foe.Species?.Name ?? foe.Nickname}|{foe.CurrentHp}/{foe.MaxStats[(int)Stat.HP]}|[from] ability: Bad Dreams|[of] {owner.Species?.Name ?? owner.Nickname}");
            }
        }
    }
}
