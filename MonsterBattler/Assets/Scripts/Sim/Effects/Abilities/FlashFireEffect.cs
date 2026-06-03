using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Flash Fire: absorbs incoming Fire moves (no damage) and grants the owner a 1.5× base-power
    /// multiplier on their own Fire moves for the rest of their stay on the field.
    /// </summary>
    public sealed class FlashFireEffect : Effect
    {
        public override string EffectId => "flashfire";
        public override string DisplayName => "Flash Fire";

        const string Tag = "flashfire";

        public override void OnTryHit(TryHitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            if (ev.Move?.Type != MonType.Fire) return;
            ev.Blocked = true;
            ev.BlockReason = "Flash Fire";
            if (owner.Tags.Add(Tag))
                ev.Battle.Log.Raw($"|-start|{owner.Species?.Name ?? owner.Nickname}|ability: Flash Fire");
        }

        public override void OnBasePower(BasePowerEvent ev, Pokemon owner)
        {
            if (owner != ev.User) return;
            if (ev.Move?.Type != MonType.Fire) return;
            if (!owner.Tags.Contains(Tag)) return;
            ev.BasePower = ev.BasePower * 3 / 2;
        }
    }
}
