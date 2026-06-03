using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>
    /// King's Shield: like Protect but only blocks attacking moves. Contact attackers get
    /// Atk lowered by 1 stage. The stance revert is handled by <see cref="Abilities.StanceChangeEffect"/>.
    /// </summary>
    public sealed class KingsShieldMoveEffect : Effect
    {
        public override string EffectId => "kingsshieldmove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            if (ev.User == null || ev.User.IsFainted) return;
            ev.Battle.AddVolatile(ev.User, "kingsshield", singleTurn: true);
        }
    }

    /// <summary>
    /// Active volatile that blocks attacking moves and lowers contact attackers' Atk by 1.
    /// </summary>
    public sealed class KingsShieldVolatile : Effect
    {
        public override string EffectId => "kingsshield";
        public override string DisplayName => "King's Shield";

        public override void OnTryHit(TryHitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move == null) return;
            if (ev.Move.Category == MoveCategory.Status) return; // Lets status moves through.
            ev.Blocked = true;
            ev.BlockReason = "King's Shield";
            if (ev.Move.Contact && ev.User != null && !ev.User.IsFainted)
                ev.Battle.BoostStat(ev.User, Stat.Atk, -1);
        }
    }
}
