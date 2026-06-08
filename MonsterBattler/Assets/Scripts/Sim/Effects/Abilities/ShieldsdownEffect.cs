using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Shields Down: Minior breaks to its Core form once it drops below half HP.</summary>
    public sealed class ShieldsDownEffect : Effect
    {
        public override string EffectId => "shieldsdown";
        public override string DisplayName => "Shields Down";
        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || owner.IsFainted) return;
            if (owner.Species?.Id == "miniormeteor" && owner.CurrentHp * 2 < owner.MaxStats[(int)Stat.HP])
                ev.Battle.ChangeForm(owner, "minior");
        }
    }
}
