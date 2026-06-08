using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Items
{
    /// <summary>Chesto Berry: wakes the holder from sleep, then is consumed.</summary>
    public sealed class ChestoBerryEffect : Effect
    {
        public override string EffectId => "chestoberry";
        public override string DisplayName => "Chesto Berry";
        public override void OnResidual(ResidualEvent ev, Pokemon owner)
        {
            if (ev.Battle.BerriesSuppressed(owner)) return;
            if (owner != ev.Target || owner.Status != StatusCondition.Sleep) return;
            owner.Status = StatusCondition.None; owner.StatusEffect = null; owner.SleepTurnsLeft = 0;
            ev.Battle.Log.Raw($"|-curestatus|{owner.Species?.Name ?? owner.Nickname}|slp|[from] item: Chesto Berry");
            ev.Battle.ConsumeItem(owner, "item: Chesto Berry");
        }
    }
}
