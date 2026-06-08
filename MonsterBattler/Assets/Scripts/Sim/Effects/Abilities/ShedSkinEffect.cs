using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Shed Skin: 1/3 chance each turn end to cure the owner's non-volatile status.</summary>
    public sealed class ShedSkinEffect : Effect
    {
        public override string EffectId => "shedskin";
        public override string DisplayName => "Shed Skin";

        public override void OnResidual(ResidualEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || owner.IsFainted) return;
            if (owner.Status == StatusCondition.None) return;
            if (!ev.Battle.Prng.Chance(1, 3)) return;
            ev.Battle.Log.Raw($"|-curestatus|{owner.Species?.Name ?? owner.Nickname}|{owner.Status.ToString().ToLower()}|[from] ability: Shed Skin");
            owner.Status = StatusCondition.None;
            owner.StatusEffect = null;
            owner.ToxicCounter = 0;
            owner.SleepTurnsLeft = 0;
        }
    }
}
