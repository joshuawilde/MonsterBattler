namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Gulp Missile: after Surf/Dive the gulping form retaliates when hit (logic in UseMove + GulpMissileVolatile).</summary>
    public sealed class GulpmissileEffect : Effect
    {
        public override string EffectId => "gulpmissile";
        public override string DisplayName => "Gulpmissile";
    }
}
