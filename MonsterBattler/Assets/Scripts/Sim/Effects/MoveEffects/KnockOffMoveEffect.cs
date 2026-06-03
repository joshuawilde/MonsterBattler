using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>
    /// Knock Off: ×1.5 base power against a target holding an item, and removes the held item
    /// on a damaging hit. (Skips mega stones / form-locked items once those exist.)
    /// </summary>
    public sealed class KnockOffMoveEffect : Effect
    {
        public override string EffectId => "knockoffmove";

        public override void OnBasePower(BasePowerEvent ev, Pokemon owner)
        {
            if (ev.Target != null && ev.Target.Item != null)
                ev.BasePower = ev.BasePower * 3 / 2;
        }

        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            var t = ev.Target;
            if (t == null || t.IsFainted || t.Item == null) return;
            ev.Battle.Log.Raw($"|-enditem|{t.Species?.Name ?? t.Nickname}|{t.Item.Name}|[from] move: Knock Off");
            t.Item = null;
            t.ItemEffect = null;
        }
    }
}
