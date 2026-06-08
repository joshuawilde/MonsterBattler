using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Haze: resets every stat stage on both active mons to zero.</summary>
    public sealed class HazeMoveEffect : Effect
    {
        public override string EffectId => "hazemove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            foreach (var side in ev.Battle.Sides)
            {
                if (side.ActiveSlots.Count == 0) continue;
                var m = side.ActiveSlots[0];
                if (m == null) continue;
                for (int i = 1; i < m.StatStages.Length; i++) m.StatStages[i] = 0; // skip HP (index 0)
            }
            ev.Battle.Log.Raw("|-clearallboost");
        }
    }
}
