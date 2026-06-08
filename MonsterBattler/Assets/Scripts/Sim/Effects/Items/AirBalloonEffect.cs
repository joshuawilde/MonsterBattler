using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Items
{
    /// <summary>Air Balloon: grants Ground immunity; pops (is consumed) when the holder is hit by any move.</summary>
    public sealed class AirBalloonEffect : Effect
    {
        public override string EffectId => "airballoon";
        public override string DisplayName => "Air Balloon";
        public override void OnTryHit(TryHitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move == null) return;
            if (ev.Move.Type == MonType.Ground && ev.Move.Category != MoveCategory.Status)
            { ev.Blocked = true; ev.BlockReason = "Air Balloon"; }
        }
        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || owner.Item == null) return;
            ev.Battle.Log.Raw($"|-end|{owner.Species?.Name ?? owner.Nickname}|Air Balloon");
            ev.Battle.ConsumeItem(owner, "item: Air Balloon");
        }
    }
}
