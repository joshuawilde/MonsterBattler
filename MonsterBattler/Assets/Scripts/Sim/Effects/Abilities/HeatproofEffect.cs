using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Heatproof: halves Fire-type damage taken.</summary>
    public sealed class HeatproofEffect : Effect
    {
        public override string EffectId => "heatproof";
        public override string DisplayName => "Heatproof";

        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move == null) return;
            if (ev.Move.Type != MonType.Fire) return;
            ev.Damage /= 2;
        }
    }
}
