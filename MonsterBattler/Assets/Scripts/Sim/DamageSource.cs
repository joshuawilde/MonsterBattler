namespace MonsterBattler.Sim
{
    /// <summary>
    /// Where an instance of damage came from. Used by abilities like Magic Guard / Rocky Helmet /
    /// Overcoat / Sand Force to selectively allow or block damage.
    /// </summary>
    public enum DamageSource
    {
        /// <summary>A direct move hit. Magic Guard does NOT block this.</summary>
        Move,
        Recoil,
        Sandstorm,
        Burn,
        Poison,
        Toxic,
        LeechSeed,
        Confusion,
        LifeOrb,
        Substitute,
        Hazard,
        TrappingMove,
        Curse,
        SolarPower,
        Other,
    }
}
