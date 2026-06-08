namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Libero: changes the user's type to its move's type each switch-in (logic in UseMove).</summary>
    public sealed class LiberoEffect : Effect
    {
        public override string EffectId => "libero";
        public override string DisplayName => "Libero";
    }
}
