using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Schooling: on switch-in (level 20+, HP above 1/4 max) and at start of each turn,
    /// swap into school form. When HP drops to or below 1/4, revert to solo form.
    /// </summary>
    public sealed class SchoolingEffect : Effect
    {
        public override string EffectId => "schooling";
        public override string DisplayName => "Schooling";

        public override void OnSwitchIn(SwitchInEvent ev, Pokemon owner)
        {
            if (owner != ev.Pokemon) return;
            TryUpdateForm(ev.Battle, owner);
        }

        public override void OnDamagingHit(HitEvent ev, Pokemon owner)
        {
            if (owner != ev.Target) return;
            TryUpdateForm(ev.Battle, owner);
        }

        static void TryUpdateForm(Battle battle, Pokemon mon)
        {
            if (mon.Level < 20 || mon.IsFainted) return;
            int max = mon.MaxStats[(int)Stat.HP];
            bool aboveThreshold = mon.CurrentHp * 4 > max;
            bool isSolo = mon.Species?.Id == "wishiwashi";
            bool isSchool = mon.Species?.Id == "wishiwashischool";
            if (aboveThreshold && isSolo) battle.ChangeForm(mon, "wishiwashischool");
            else if (!aboveThreshold && isSchool) battle.ChangeForm(mon, "wishiwashi");
        }
    }
}
