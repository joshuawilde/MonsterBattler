namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Infiltrator: ignores the target's screens and Substitute (logic in screen conditions / UseMove).</summary>
    public sealed class InfiltratorEffect : Effect
    {
        public override string EffectId => "infiltrator";
        public override string DisplayName => "Infiltrator";
    }
}
