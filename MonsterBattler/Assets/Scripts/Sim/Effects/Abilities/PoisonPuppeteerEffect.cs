namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Poison Puppeteer: confuses a foe the owner poisons (logic in Battle.ApplyStatus).</summary>
    public sealed class PoisonPuppeteerEffect : Effect
    {
        public override string EffectId => "poisonpuppeteer";
        public override string DisplayName => "Poison Puppeteer";
    }
}
