namespace MonsterBattler.Sim.Effects.SideConditions
{
    /// <summary>
    /// Mist: prevents opposing stat-lowering for 5 turns. We don't currently have a
    /// "TryStatDrop" event — once that's wired the side condition will block via that.
    /// For now it just exists as a placeholder so the move sets up and ticks down.
    /// </summary>
    public sealed class MistCondition : Effect
    {
        public override string EffectId => "mist";
        public override string DisplayName => "Mist";
    }
}
