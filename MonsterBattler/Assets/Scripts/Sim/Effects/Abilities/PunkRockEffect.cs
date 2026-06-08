using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Punk Rock: the owner's Sound-based moves get a 1.3× base-power boost, and Sound-based
    /// moves used against the owner deal half damage.
    /// </summary>
    public sealed class PunkRockEffect : Effect
    {
        public override string EffectId => "punkrock";
        public override string DisplayName => "Punk Rock";

        public override void OnBasePower(BasePowerEvent ev, Pokemon owner)
        {
            if (owner != ev.User) return;
            if (ev.Move == null || !ev.Move.Sound) return;
            ev.BasePower = ev.BasePower * 13 / 10;
        }

        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            if (ev.Move == null || !ev.Move.Sound) return;
            ev.Damage = ev.Damage / 2;
        }
    }
}
