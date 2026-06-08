using System.Collections.Generic;
using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>Court Change: swaps every side condition (hazards, screens, Tailwind, etc.)
    /// between the two sides.</summary>
    public sealed class CourtChangeMoveEffect : Effect
    {
        public override string EffectId => "courtchangemove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var sides = ev.Battle.Sides;
            if (sides == null || sides.Length < 2) return;
            var a = sides[0];
            var b = sides[1];
            if (a == null || b == null) return;

            var tmp = a.Conditions;
            a.Conditions = b.Conditions;
            b.Conditions = tmp;

            ev.Battle.Log.Raw($"|-activate|{Name(ev.User)}|move: Court Change");
        }

        static string Name(Pokemon m) => m?.Species?.Name ?? m?.Nickname ?? "?";
    }
}
