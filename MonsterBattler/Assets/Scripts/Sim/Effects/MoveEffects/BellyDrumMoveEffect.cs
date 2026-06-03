using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Belly Drum: spend 1/2 max HP, set Atk stage to +6. Fails if HP cost would faint the user.</summary>
    public sealed class BellyDrumMoveEffect : Effect
    {
        public override string EffectId => "bellydrummove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var u = ev.User;
            if (u == null || u.IsFainted) return;
            int max = u.MaxStats[(int)Stat.HP];
            int cost = max / 2;
            if (u.CurrentHp <= cost || u.StatStages[(int)Stat.Atk] >= 6)
            {
                ev.Battle.Log.Raw($"|-fail|{u.Species?.Name ?? u.Nickname}");
                return;
            }
            ev.Battle.ApplyDamage(u, cost);
            ev.Battle.Log.Raw($"|-damage|{u.Species?.Name ?? u.Nickname}|{u.CurrentHp}/{max}|[from] move: Belly Drum");
            u.StatStages[(int)Stat.Atk] = 6;
            ev.Battle.Log.Raw($"|-setboost|{u.Species?.Name ?? u.Nickname}|atk|6");
        }
    }
}
