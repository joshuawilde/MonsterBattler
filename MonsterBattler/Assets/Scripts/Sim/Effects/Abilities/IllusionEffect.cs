namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Illusion: disguises the owner as the last party member to deceive the opponent. Inert ONLY because
    /// our AI reads ground-truth state; true parity needs an AI perception/"believed-state" layer (see project-parity-gaps).</summary>
    public sealed class IllusionEffect : Effect
    {
        public override string EffectId => "illusion";
        public override string DisplayName => "Illusion";
    }
}
