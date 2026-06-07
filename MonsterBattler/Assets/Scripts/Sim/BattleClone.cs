using System.Collections.Generic;
using MonsterBattler.Sim.Data;

namespace MonsterBattler.Sim
{
    /// <summary>
    /// Deep-clone of the mutable battle state, for AI lookahead/search. Immutable data is SHARED
    /// (Dex, species/move/ability/item data, Effect singletons); everything the engine mutates during
    /// a turn is copied (HP, status, stat stages, PP, volatiles, side/field conditions, PRNG). After
    /// copying, intra-battle Pokemon references (ActiveSlots, LastDamageSource, VolatileSlot.Source)
    /// are relinked to the cloned mons so simulating the clone never touches the original.
    /// </summary>
    public sealed partial class Battle
    {
        // Private ctor used only by Clone (sets the readonly Dex/Prng directly).
        Battle(Dex dex, Prng prng)
        {
            Dex = dex;
            Prng = prng;
        }

        /// <param name="newSeed">If set, the clone's PRNG is reseeded (sample a different chance line);
        /// otherwise the PRNG state is copied exactly.</param>
        public Battle Clone(ulong? newSeed = null)
        {
            var clone = new Battle(Dex, newSeed.HasValue ? new Prng(newSeed.Value) : Prng.Clone());
            clone.TurnNumber = TurnNumber;
            clone.IsFinished = IsFinished;
            clone.WinningSide = WinningSide;

            clone.Field.Weather = Field.Weather;
            clone.Field.WeatherTurnsLeft = Field.WeatherTurnsLeft;
            clone.Field.Terrain = Field.Terrain;
            clone.Field.TerrainTurnsLeft = Field.TerrainTurnsLeft;
            foreach (var kv in Field.Conditions)
                clone.Field.Conditions[kv.Key] = new FieldCondition { Id = kv.Value.Id, TurnsLeft = kv.Value.TurnsLeft, Data = kv.Value.Data };

            var map = new Dictionary<Pokemon, Pokemon>();
            for (int s = 0; s < 2; s++)
            {
                var src = Sides[s];
                var dst = new Side { Index = src.Index, Name = src.Name, HasUsedTera = src.HasUsedTera };
                foreach (var mon in src.Team)
                {
                    var cm = ClonePokemon(mon);
                    map[mon] = cm;
                    dst.Team.Add(cm);
                }
                foreach (var kv in src.Conditions)
                {
                    var sc = kv.Value;
                    dst.Conditions[kv.Key] = new SideCondition { Id = sc.Id, Effect = sc.Effect, TurnsLeft = sc.TurnsLeft, Layers = sc.Layers, Data = sc.Data };
                }
                clone.Sides[s] = dst;
            }

            // Relink Pokemon references through the orig->clone map.
            for (int s = 0; s < 2; s++)
                foreach (var mon in Sides[s].ActiveSlots)
                    clone.Sides[s].ActiveSlots.Add(map[mon]);

            foreach (var cm in map.Values)
            {
                if (cm.LastDamageSource != null)
                    cm.LastDamageSource = map.TryGetValue(cm.LastDamageSource, out var ls) ? ls : null;
                foreach (var v in cm.Volatiles.Values)
                    if (v.Source != null && map.TryGetValue(v.Source, out var vs)) v.Source = vs;
            }

            return clone;
        }

        static Pokemon ClonePokemon(Pokemon m)
        {
            var c = new Pokemon
            {
                Species = m.Species, Nickname = m.Nickname, Level = m.Level, Gender = m.Gender,
                Ability = m.Ability, Item = m.Item,
                AbilityEffect = m.AbilityEffect, ItemEffect = m.ItemEffect, StatusEffect = m.StatusEffect,
                CurrentHp = m.CurrentHp, Status = m.Status, SleepTurnsLeft = m.SleepTurnsLeft, ToxicCounter = m.ToxicCounter,
                LastMoveUsed = m.LastMoveUsed, LockedMoveId = m.LockedMoveId,
                LastDamageAmount = m.LastDamageAmount, LastDamageCategory = m.LastDamageCategory,
                LastDamageSource = m.LastDamageSource, // relinked by caller
                LastDamageTurn = m.LastDamageTurn,
                TeraType = m.TeraType, IsTerastallized = m.IsTerastallized, IsActive = m.IsActive,
            };
            System.Array.Copy(m.IVs, c.IVs, m.IVs.Length);
            System.Array.Copy(m.EVs, c.EVs, m.EVs.Length);
            System.Array.Copy(m.MaxStats, c.MaxStats, m.MaxStats.Length);
            System.Array.Copy(m.StatStages, c.StatStages, m.StatStages.Length);
            foreach (var t in m.Tags) c.Tags.Add(t);
            foreach (var ms in m.Moves)
                c.Moves.Add(new MoveSlot { Move = ms.Move, Pp = ms.Pp, MaxPp = ms.MaxPp, Disabled = ms.Disabled });
            foreach (var kv in m.Volatiles)
            {
                var v = kv.Value;
                c.Volatiles[kv.Key] = new Effects.VolatileSlot
                {
                    Effect = v.Effect, Source = v.Source, Turns = v.Turns,
                    Counter = v.Counter, SingleTurn = v.SingleTurn, Extra = v.Extra,
                };
            }
            return c;
        }
    }
}
