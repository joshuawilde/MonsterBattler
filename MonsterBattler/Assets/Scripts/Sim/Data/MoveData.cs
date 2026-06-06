namespace MonsterBattler.Sim.Data
{
    /// <summary>
    /// Immutable move definition. The pure-data fields cover ~60% of gen9 moves verbatim;
    /// the remaining ~40% additionally attach an Effect class (see Effects/) that hooks into
    /// the battle event system.
    /// </summary>
    public sealed class MoveData
    {
        public string Id;             // e.g. "tackle"
        public string Name;
        public MonType Type;
        public MoveCategory Category;
        public int BasePower;         // 0 for status moves
        public int Accuracy;          // 0..100, or 0 meaning "never miss"
        public int Pp;                // base PP, before PP Ups
        public int Priority;          // -7..+5
        public int CritRatio;         // 0 = base 1/24, 1 = 1/8, 2 = 1/2, 3+ = guaranteed

        // Recoil: user takes Damage * num / den. Default (0/0) = no recoil.
        public int RecoilNum;
        public int RecoilDen;
        // Drain: user heals Damage * num / den.
        public int DrainNum;
        public int DrainDen;
        // Self-KO moves (Explosion, Self-Destruct, Final Gambit, Memento).
        public bool SelfKO;
        // Pivot moves — user switches out after the move connects if they have an available bench.
        public bool PivotsOut;
        // Multi-hit: rolls 2–5 hits when MultihitMax > 0 (or fixed = MultihitMin when min == max).
        public int MultihitMin;
        public int MultihitMax;
        // 0..100 probability of flinching the target after a damaging hit.
        public int FlinchChance;
        /// <summary>Two-turn charge moves: spend turn 1 charging, hit on turn 2.</summary>
        public bool TwoTurn;

        public MoveTarget Target = MoveTarget.Normal;

        // Bitfield-ish flags. TODO: replace with a [Flags] enum once we know the full set.
        public bool Contact;
        public bool Protect;          // blocked by Protect/Detect
        public bool Sound;
        public bool Punch;
        public bool Bite;
        public bool Slicing;
        public bool Wind;
        public bool Bullet;

        /// <summary>Optional name of an effect class that supplies callback overrides (see Effects/).</summary>
        public string EffectId;

        /// <summary>One-line human description (Showdown's shortDesc), e.g. "10% chance to burn the target."</summary>
        public string ShortDesc;

        /// <summary>
        /// Chance-based secondary effects (Flamethrower's 10% burn, Crunch's 20% −Def, …).
        /// Suppressed by Sheer Force, blocked on the target by Shield Dust, doubled by Serene Grace.
        /// Pure-flinch secondaries live in <see cref="FlinchChance"/> instead. Null if none.
        /// </summary>
        public MoveSecondary[] Secondaries;

        /// <summary>Guaranteed self stat changes after the move connects (Close Combat, Overheat,
        /// Draco Meteor, …). NOT affected by Sheer Force / Shield Dust. Null if none.</summary>
        public StatChange[] SelfBoosts;
    }

    /// <summary>One chance-based secondary effect attached to a move.</summary>
    public sealed class MoveSecondary
    {
        public int Chance;            // 0..100; treated as guaranteed when <= 0
        public string Status;         // "brn"/"par"/"psn"/"tox"/"frz"/"slp", or null
        public string Volatile;       // "confusion" etc., or null (flinch uses MoveData.FlinchChance)
        public StatChange[] TargetBoosts; // stat changes applied to the target
        public StatChange[] SelfBoosts;   // stat changes applied to the user (e.g. Charge Beam)
    }

    /// <summary>A single stat-stage change.</summary>
    public struct StatChange
    {
        public Stat Stat;
        public int Delta;
    }

    public enum MoveTarget
    {
        Normal,             // single adjacent foe
        Self,
        AllyOrSelf,
        AdjacentAlly,
        AllAdjacentFoes,
        AllAdjacent,
        All,
        AllySide,
        FoeSide,
        Field,
        RandomNormal,
    }
}
