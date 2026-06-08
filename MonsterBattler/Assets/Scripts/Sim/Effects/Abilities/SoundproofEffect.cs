using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Soundproof: immune to sound moves.</summary>
    public sealed class SoundproofEffect : Effect
    {
        public override string EffectId => "soundproof";
        public override string DisplayName => "Soundproof";
        public override void OnTryHit(TryHitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move == null || !ev.Move.Sound) return;
            ev.Blocked = true; ev.BlockReason = "Soundproof";
        }
    }
}
