using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>
    /// Trick Room: toggles a 5-turn field condition that reverses Speed ordering. Slower mons
    /// move first while it's active.
    /// </summary>
    public sealed class TrickRoomMoveEffect : Effect
    {
        public override string EffectId => "trickroommove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var f = ev.Battle.Field;
            if (f.Conditions.ContainsKey("trickroom"))
            {
                f.Conditions.Remove("trickroom");
                ev.Battle.Log.Raw($"|-fieldend|move: Trick Room");
            }
            else
            {
                f.Conditions["trickroom"] = new FieldCondition { Id = "trickroom", TurnsLeft = 5 };
                ev.Battle.Log.Raw($"|-fieldstart|move: Trick Room");
            }
        }
    }
}
