using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Sheer Force: moves that have a chance-based secondary lose that secondary but gain ×1.3 base
    /// power. The secondary suppression is handled by reference in <see cref="Battle.ApplySecondaries"/>;
    /// this hook supplies the power boost.
    /// </summary>
    public sealed class SheerForceEffect : Effect
    {
        public override string EffectId => "sheerforce";
        public override string DisplayName => "Sheer Force";

        public override void OnBasePower(BasePowerEvent ev, Pokemon owner)
        {
            if (owner != ev.User || ev.Move?.Secondaries == null || ev.Move.Secondaries.Length == 0) return;
            ev.BasePower = ev.BasePower * 13 / 10;
        }
    }
}
