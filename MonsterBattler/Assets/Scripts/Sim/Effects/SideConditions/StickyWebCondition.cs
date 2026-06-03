using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.SideConditions
{
    /// <summary>Sticky Web: each Pokemon switching in onto this side loses 1 Speed stage. Flying/Levitate immune.</summary>
    public sealed class StickyWebCondition : Effect
    {
        public override string EffectId => "stickyweb";
        public override string DisplayName => "Sticky Web";

        public override void OnSwitchIn(SwitchInEvent ev, Pokemon owner)
        {
            var mon = owner;
            if (mon == null || mon.IsFainted || mon.Species == null) return;
            if (IsType(mon, MonType.Flying)) return;
            if (mon.AbilityEffect is Abilities.LevitateEffect) return;
            ev.Battle.BoostStat(mon, Stat.Spe, -1);
        }

        static bool IsType(Pokemon m, MonType t) => m?.Species != null && (m.Species.Type1 == t || m.Species.Type2 == t);
    }
}
