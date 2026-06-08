using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Supreme Overlord: the owner's Attack and Special Attack are boosted by 10% for each
    /// fainted ally on the owner's team (×(1 + 0.1 * faintedCount)).
    /// </summary>
    public sealed class SupremeOverlordEffect : Effect
    {
        public override string EffectId => "supremeoverlord";
        public override string DisplayName => "Supreme Overlord";

        public override void OnModifyAtk(StatModifyEvent ev, Pokemon owner)
        {
            if (owner != ev.Owner) return;
            ev.Value = ev.Value * (10 + Fainted(ev.Battle, owner)) / 10;
        }

        public override void OnModifySpA(StatModifyEvent ev, Pokemon owner)
        {
            if (owner != ev.Owner) return;
            ev.Value = ev.Value * (10 + Fainted(ev.Battle, owner)) / 10;
        }

        static int Fainted(Battle battle, Pokemon owner)
        {
            var side = battle.SideOf(owner);
            if (side == null) return 0;
            int count = 0;
            foreach (var mon in side.Team)
            {
                if (mon != null && mon != owner && mon.IsFainted) count++;
            }
            if (count > 5) count = 5;
            return count;
        }
    }
}
