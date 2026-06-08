using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Items
{
    /// <summary>Lum Berry: cures any non-volatile status (and confusion), then is consumed.</summary>
    public sealed class LumBerryEffect : Effect
    {
        public override string EffectId => "lumberry";
        public override string DisplayName => "Lum Berry";
        public override void OnResidual(ResidualEvent ev, Pokemon owner)
        {
            if (ev.Battle.BerriesSuppressed(owner)) return;
            if (owner != ev.Target || owner.IsFainted) return;
            bool confused = owner.GetVolatile("confusion") != null;
            if (owner.Status == StatusCondition.None && !confused) return;
            owner.Status = StatusCondition.None; owner.StatusEffect = null; owner.SleepTurnsLeft = 0;
            if (confused) ev.Battle.RemoveVolatile(owner, "confusion");
            ev.Battle.Log.Raw($"|-curestatus|{owner.Species?.Name ?? owner.Nickname}|[from] item: Lum Berry");
            ev.Battle.ConsumeItem(owner, "item: Lum Berry");
        }
    }
}
