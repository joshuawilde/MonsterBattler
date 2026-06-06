using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Volatiles
{
    /// <summary>
    /// The Protosynthesis / Quark Drive boost. Shared by both abilities (and Booster Energy): the
    /// holder's highest non-HP stat is boosted ×1.3 (×1.5 if that stat is Speed).
    ///
    /// Per-mon state on the <see cref="VolatileSlot"/>:
    ///   • Counter = (int)the boosted Stat
    ///   • Extra   = "sun" | "terrain" | "booster" — the activation source
    /// Weather/terrain sources are re-checked lazily each time a stat is queried, so the boost
    /// naturally lapses when the sun/Electric Terrain ends and resumes if it returns (Booster
    /// Energy sources persist unconditionally).
    /// </summary>
    public sealed class ParadoxBoostVolatile : Effect
    {
        public override string EffectId => "paradoxboost";
        public override string DisplayName => "Paradox Boost";

        public override void OnModifyAtk(StatModifyEvent ev, Pokemon owner) => Mod(ev, owner, Stat.Atk);
        public override void OnModifyDef(StatModifyEvent ev, Pokemon owner) => Mod(ev, owner, Stat.Def);
        public override void OnModifySpA(StatModifyEvent ev, Pokemon owner) => Mod(ev, owner, Stat.SpA);
        public override void OnModifySpD(StatModifyEvent ev, Pokemon owner) => Mod(ev, owner, Stat.SpD);
        public override void OnModifySpe(StatModifyEvent ev, Pokemon owner) => Mod(ev, owner, Stat.Spe);

        static void Mod(StatModifyEvent ev, Pokemon owner, Stat stat)
        {
            if (owner != ev.Owner) return;
            var slot = owner.GetVolatile("paradoxboost");
            if (slot == null || slot.Counter != (int)stat || !StillActive(slot, ev.Battle)) return;
            ev.Value = stat == Stat.Spe ? ev.Value * 3 / 2 : ev.Value * 13 / 10;
        }

        static bool StillActive(VolatileSlot slot, Battle battle)
        {
            switch (slot.Extra as string)
            {
                case "sun":     return battle.Field.Weather == Weather.Sun || battle.Field.Weather == Weather.HarshSun;
                case "terrain": return battle.Field.Terrain == Terrain.Electric;
                default:        return true; // Booster Energy — persists.
            }
        }

        /// <summary>Activate (or re-evaluate) the boost on <paramref name="mon"/> from a given source.</summary>
        public static void Activate(Pokemon mon, Battle battle, string source, string abilityName)
        {
            var slot = mon.GetVolatile("paradoxboost") ?? battle.AddVolatile(mon, "paradoxboost");
            if (slot == null) return;
            var stat = HighestStat(mon);
            slot.Counter = (int)stat;
            slot.Extra = source;
            battle.Log.Raw($"|-activate|{mon.Species?.Name ?? mon.Nickname}|ability: {abilityName}|[fromstat] {stat}");
        }

        static Stat HighestStat(Pokemon m)
        {
            var best = Stat.Atk;
            int bestVal = m.MaxStats[(int)Stat.Atk];
            foreach (var s in new[] { Stat.Def, Stat.SpA, Stat.SpD, Stat.Spe })
            {
                if (m.MaxStats[(int)s] > bestVal) { bestVal = m.MaxStats[(int)s]; best = s; }
            }
            return best;
        }
    }
}
