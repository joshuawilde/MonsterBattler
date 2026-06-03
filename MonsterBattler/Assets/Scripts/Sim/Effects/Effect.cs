using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects
{
    /// <summary>
    /// Base class for every gameplay effect (abilities, items, statuses, side conditions,
    /// field conditions, volatile effects, move secondaries). Override the hooks you care about;
    /// the default implementations are no-ops.
    ///
    /// Hooks receive an <c>owner</c> parameter: the Pokemon this effect is attached to, or
    /// null for move/side/field effects with no Pokemon owner. Effects check `owner` against
    /// the event's User/Target to know which side they're acting on.
    /// </summary>
    public abstract class Effect : IEffectSource
    {
        public abstract string EffectId { get; }
        public virtual string DisplayName => EffectId;

        // -------- Move pipeline --------
        public virtual void OnTryHit(TryHitEvent ev, Pokemon owner) { }
        public virtual void OnHit(HitEvent ev, Pokemon owner) { }
        public virtual void OnDamagingHit(HitEvent ev, Pokemon owner) { }
        public virtual void OnAfterMove(AfterMoveEvent ev, Pokemon owner) { }

        // -------- Damage modifiers --------
        public virtual void OnBasePower(BasePowerEvent ev, Pokemon owner) { }
        public virtual void OnModifyAtk(StatModifyEvent ev, Pokemon owner) { }
        public virtual void OnModifyDef(StatModifyEvent ev, Pokemon owner) { }
        public virtual void OnModifySpA(StatModifyEvent ev, Pokemon owner) { }
        public virtual void OnModifySpD(StatModifyEvent ev, Pokemon owner) { }
        public virtual void OnModifySpe(StatModifyEvent ev, Pokemon owner) { }
        public virtual void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner) { }

        // -------- Lifecycle --------
        public virtual void OnSwitchIn(SwitchInEvent ev, Pokemon owner) { }
        public virtual void OnSwitchOut(SwitchOutEvent ev, Pokemon owner) { }
        public virtual void OnFaint(FaintEvent ev, Pokemon owner) { }

        // -------- End-of-turn --------
        public virtual void OnResidual(ResidualEvent ev, Pokemon owner) { }
    }
}
