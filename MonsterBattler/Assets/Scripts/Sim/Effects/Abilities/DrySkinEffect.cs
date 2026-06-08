using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Dry Skin: immune to Water (heals 1/4 max HP instead), takes 1.25× Fire damage, heals
    /// 1/8 max HP each turn in Rain, and loses 1/8 max HP each turn in Sun.
    /// </summary>
    public sealed class DrySkinEffect : Effect
    {
        public override string EffectId => "dryskin";
        public override string DisplayName => "Dry Skin";

        public override void OnTryHit(TryHitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move?.Type != MonType.Water) return;
            ev.Blocked = true;
            ev.BlockReason = "Dry Skin";
            int max = owner.MaxStats[(int)Stat.HP];
            int heal = System.Math.Min(max / 4, max - owner.CurrentHp);
            if (heal <= 0) return;
            owner.CurrentHp += heal;
            ev.Battle.Log.Raw($"|-heal|{owner.Species?.Name ?? owner.Nickname}|{owner.CurrentHp}/{max}|[from] ability: Dry Skin");
        }

        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move == null) return;
            if (ev.Move.Type != MonType.Fire) return;
            ev.Damage = ev.Damage * 5 / 4;
        }

        public override void OnResidual(ResidualEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || owner.IsFainted) return;
            var weather = ev.Battle.Field.Weather;
            int max = owner.MaxStats[(int)Stat.HP];
            if (weather == Weather.Rain)
            {
                int heal = System.Math.Min(max / 8, max - owner.CurrentHp);
                if (heal <= 0) return;
                owner.CurrentHp += heal;
                ev.Battle.Log.Raw($"|-heal|{owner.Species?.Name ?? owner.Nickname}|{owner.CurrentHp}/{max}|[from] ability: Dry Skin");
            }
            else if (weather == Weather.Sun)
            {
                int dmg = System.Math.Max(1, max / 8);
                ev.Battle.ApplyDamage(owner, dmg);
                ev.Battle.Log.Raw($"|-damage|{owner.Species?.Name ?? owner.Nickname}|{owner.CurrentHp}/{max}|[from] ability: Dry Skin");
            }
        }
    }
}
