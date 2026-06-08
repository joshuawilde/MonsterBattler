using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    public sealed class PurifyingSaltEffect : Effect
    {
        public override string EffectId => "purifyingsalt";
        public override string DisplayName => "Purifying Salt";
        public override void OnTryStatus(TryStatusEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            
            ev.Blocked = true; ev.BlockReason = "Purifying Salt";
        }
    }
}
