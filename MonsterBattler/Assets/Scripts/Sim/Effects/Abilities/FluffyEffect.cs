using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Fluffy: halves damage from contact moves, but doubles damage taken from Fire-type moves.
    /// (A contacting Fire move nets the same damage — half then double.)
    /// </summary>
    public sealed class FluffyEffect : Effect
    {
        public override string EffectId => "fluffy";
        public override string DisplayName => "Fluffy";

        public override void OnModifyDamage(ModifyDamageEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || ev.Move == null) return;
            if (ev.Move.Contact) ev.Damage /= 2;
            if (ev.Move.Type == MonType.Fire) ev.Damage *= 2;
        }
    }
}
