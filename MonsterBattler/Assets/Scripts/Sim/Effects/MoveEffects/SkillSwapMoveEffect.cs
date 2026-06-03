using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Skill Swap: user and target trade abilities. Fails if either side has none.</summary>
    public sealed class SkillSwapMoveEffect : Effect
    {
        public override string EffectId => "skillswapmove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var u = ev.User; var t = ev.Target;
            if (u == null || t == null || u.AbilityEffect == null || t.AbilityEffect == null)
            {
                ev.Battle.Log.Raw($"|-fail|{u?.Species?.Name ?? u?.Nickname}");
                return;
            }
            (u.Ability, t.Ability) = (t.Ability, u.Ability);
            (u.AbilityEffect, t.AbilityEffect) = (t.AbilityEffect, u.AbilityEffect);
            ev.Battle.Log.Raw($"|-ability|{u.Species?.Name ?? u.Nickname}|{u.AbilityEffect.DisplayName}|[from] move: Skill Swap");
            ev.Battle.Log.Raw($"|-ability|{t.Species?.Name ?? t.Nickname}|{t.AbilityEffect.DisplayName}|[from] move: Skill Swap");
        }
    }
}
