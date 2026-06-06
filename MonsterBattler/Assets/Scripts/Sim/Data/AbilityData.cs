namespace MonsterBattler.Sim.Data
{
    public sealed class AbilityData
    {
        public string Id;       // e.g. "blaze"
        public string Name;
        /// <summary>Name of the effect class that registers this ability's hooks.</summary>
        public string EffectId;
        /// <summary>One-line human description (Showdown's shortDesc).</summary>
        public string ShortDesc;
    }
}
