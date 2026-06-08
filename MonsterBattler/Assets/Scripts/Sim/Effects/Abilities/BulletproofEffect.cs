using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Bulletproof: immune to bullet moves.</summary>
    public sealed class BulletproofEffect : Effect
    {
        public override string EffectId => "bulletproof";
        public override string DisplayName => "Bulletproof";
        public override void OnTryHit(TryHitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move == null || !ev.Move.Bullet) return;
            ev.Blocked = true; ev.BlockReason = "Bulletproof";
        }
    }
}
