namespace MonsterBattler.Sim.Events
{
    /// <summary>
    /// Marker interface for anything that can register effect hooks (ability, item, status,
    /// side condition, field condition). Effects themselves carry the callback logic; the
    /// dispatcher lives on <see cref="Battle"/> (RunBasePower, RunTryHit, etc.).
    /// </summary>
    public interface IEffectSource
    {
        string EffectId { get; }
    }
}
