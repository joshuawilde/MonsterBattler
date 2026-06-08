using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Slow Start: for the first 5 turns after switching in, the owner's Attack and Speed
    /// are halved. Remaining turns are tracked as Tags ("slowstart1".."slowstart5"), which the
    /// engine clears automatically on switch out; one tag is removed each end of turn.
    /// </summary>
    public sealed class SlowStartEffect : Effect
    {
        public override string EffectId => "slowstart";
        public override string DisplayName => "Slow Start";

        static bool Active(Pokemon mon)
        {
            foreach (var t in mon.Tags) if (t.StartsWith("slowstart")) return true;
            return false;
        }

        public override void OnSwitchIn(SwitchInEvent ev, Pokemon owner)
        {
            if (owner != ev.Pokemon) return;
            for (int i = 1; i <= 5; i++) owner.Tags.Add("slowstart" + i);
        }

        public override void OnResidual(ResidualEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || owner.IsFainted) return;
            for (int i = 5; i >= 1; i--)
            {
                if (owner.Tags.Remove("slowstart" + i)) break;
            }
        }

        public override void OnModifyAtk(StatModifyEvent ev, Pokemon owner)
        {
            if (owner != ev.Owner) return;
            if (!Active(owner)) return;
            ev.Value /= 2;
        }

        public override void OnModifySpe(StatModifyEvent ev, Pokemon owner)
        {
            if (owner != ev.Owner) return;
            if (!Active(owner)) return;
            ev.Value /= 2;
        }
    }
}
