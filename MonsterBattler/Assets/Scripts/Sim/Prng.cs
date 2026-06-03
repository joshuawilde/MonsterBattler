using System;

namespace MonsterBattler.Sim
{
    /// <summary>
    /// Deterministic, seedable PRNG for the battle sim. Must NOT pull in any non-deterministic
    /// source (Time, hardware RNG, etc.) — replays and PS parity depend on it.
    ///
    /// TODO: For exact parity with Pokemon Showdown gen9 random rolls, port their PRNG
    /// algorithm verbatim (sim/prng.ts uses a custom 64-bit LCG split into two 32-bit halves).
    /// For now this is a 32-bit xorshift so we can build the engine; swap before parity testing.
    /// </summary>
    public sealed class Prng
    {
        public ulong Seed { get; private set; }
        uint _state;

        public Prng(ulong seed)
        {
            Seed = seed;
            _state = (uint)(seed ^ (seed >> 32));
            if (_state == 0) _state = 0x9E3779B9;
        }

        uint NextU32()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        /// <summary>Uniform int in [min, max).</summary>
        public int Range(int min, int max)
        {
            if (max <= min) throw new ArgumentException("max must be > min");
            return min + (int)(NextU32() % (uint)(max - min));
        }

        /// <summary>True with probability `num/den` — matches PS's `randomChance` shape.</summary>
        public bool Chance(int num, int den) => Range(0, den) < num;

        /// <summary>Uniform float in [0, 1).</summary>
        public float NextFloat() => (NextU32() & 0xFFFFFF) / (float)(1 << 24);

        public Prng Fork() => new Prng((((ulong)NextU32()) << 32) | NextU32());
    }
}
