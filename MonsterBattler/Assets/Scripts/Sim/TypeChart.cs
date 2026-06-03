namespace MonsterBattler.Sim
{
    /// <summary>
    /// Gen 9 type effectiveness chart. Source: bulbapedia "Type chart (Generation IX)".
    /// Multipliers are 0, 0.5, 1, or 2. Stellar is treated as neutral against everything
    /// except Terastallized targets (×2 vs any tera type) — that interaction will land
    /// when Tera is wired into DamageCalc; for now Stellar is neutral.
    /// </summary>
    public static class TypeChart
    {
        static readonly float[,] s_table;

        static TypeChart()
        {
            int n = System.Enum.GetValues(typeof(MonType)).Length;
            s_table = new float[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    s_table[i, j] = 1f;

            // Each entry: attacker, super-effective targets, not-very-effective targets, no-effect targets.
            Add(MonType.Normal,   SE: new MonType[] { }, NVE: new[] { MonType.Rock, MonType.Steel }, NoEff: new[] { MonType.Ghost });
            Add(MonType.Fire,     SE: new[] { MonType.Grass, MonType.Ice, MonType.Bug, MonType.Steel },
                                   NVE: new[] { MonType.Fire, MonType.Water, MonType.Rock, MonType.Dragon });
            Add(MonType.Water,    SE: new[] { MonType.Fire, MonType.Ground, MonType.Rock },
                                   NVE: new[] { MonType.Water, MonType.Grass, MonType.Dragon });
            Add(MonType.Electric, SE: new[] { MonType.Water, MonType.Flying },
                                   NVE: new[] { MonType.Electric, MonType.Grass, MonType.Dragon },
                                   NoEff: new[] { MonType.Ground });
            Add(MonType.Grass,    SE: new[] { MonType.Water, MonType.Ground, MonType.Rock },
                                   NVE: new[] { MonType.Fire, MonType.Grass, MonType.Poison, MonType.Flying, MonType.Bug, MonType.Dragon, MonType.Steel });
            Add(MonType.Ice,      SE: new[] { MonType.Grass, MonType.Ground, MonType.Flying, MonType.Dragon },
                                   NVE: new[] { MonType.Fire, MonType.Water, MonType.Ice, MonType.Steel });
            Add(MonType.Fighting, SE: new[] { MonType.Normal, MonType.Ice, MonType.Rock, MonType.Dark, MonType.Steel },
                                   NVE: new[] { MonType.Poison, MonType.Flying, MonType.Psychic, MonType.Bug, MonType.Fairy },
                                   NoEff: new[] { MonType.Ghost });
            Add(MonType.Poison,   SE: new[] { MonType.Grass, MonType.Fairy },
                                   NVE: new[] { MonType.Poison, MonType.Ground, MonType.Rock, MonType.Ghost },
                                   NoEff: new[] { MonType.Steel });
            Add(MonType.Ground,   SE: new[] { MonType.Fire, MonType.Electric, MonType.Poison, MonType.Rock, MonType.Steel },
                                   NVE: new[] { MonType.Grass, MonType.Bug },
                                   NoEff: new[] { MonType.Flying });
            Add(MonType.Flying,   SE: new[] { MonType.Grass, MonType.Fighting, MonType.Bug },
                                   NVE: new[] { MonType.Electric, MonType.Rock, MonType.Steel });
            Add(MonType.Psychic,  SE: new[] { MonType.Fighting, MonType.Poison },
                                   NVE: new[] { MonType.Psychic, MonType.Steel },
                                   NoEff: new[] { MonType.Dark });
            Add(MonType.Bug,      SE: new[] { MonType.Grass, MonType.Psychic, MonType.Dark },
                                   NVE: new[] { MonType.Fire, MonType.Fighting, MonType.Poison, MonType.Flying, MonType.Ghost, MonType.Steel, MonType.Fairy });
            Add(MonType.Rock,     SE: new[] { MonType.Fire, MonType.Ice, MonType.Flying, MonType.Bug },
                                   NVE: new[] { MonType.Fighting, MonType.Ground, MonType.Steel });
            Add(MonType.Ghost,    SE: new[] { MonType.Psychic, MonType.Ghost },
                                   NVE: new[] { MonType.Dark },
                                   NoEff: new[] { MonType.Normal });
            Add(MonType.Dragon,   SE: new[] { MonType.Dragon },
                                   NVE: new[] { MonType.Steel },
                                   NoEff: new[] { MonType.Fairy });
            Add(MonType.Dark,     SE: new[] { MonType.Psychic, MonType.Ghost },
                                   NVE: new[] { MonType.Fighting, MonType.Dark, MonType.Fairy });
            Add(MonType.Steel,    SE: new[] { MonType.Ice, MonType.Rock, MonType.Fairy },
                                   NVE: new[] { MonType.Fire, MonType.Water, MonType.Electric, MonType.Steel });
            Add(MonType.Fairy,    SE: new[] { MonType.Fighting, MonType.Dragon, MonType.Dark },
                                   NVE: new[] { MonType.Fire, MonType.Poison, MonType.Steel });
            // Stellar: neutral everywhere by default; the ×2-vs-Tera bonus is applied in DamageCalc once Tera lands.
        }

        static void Add(MonType atk, MonType[] SE = null, MonType[] NVE = null, MonType[] NoEff = null)
        {
            if (SE != null)    foreach (var d in SE)    s_table[(int)atk, (int)d] = 2f;
            if (NVE != null)   foreach (var d in NVE)   s_table[(int)atk, (int)d] = 0.5f;
            if (NoEff != null) foreach (var d in NoEff) s_table[(int)atk, (int)d] = 0f;
        }

        public static float Effectiveness(MonType attacking, MonType defending)
            => s_table[(int)attacking, (int)defending];

        public static float Effectiveness(MonType attacking, MonType defType1, MonType defType2)
        {
            var e = Effectiveness(attacking, defType1);
            if (defType2 != MonType.None && defType2 != defType1)
                e *= Effectiveness(attacking, defType2);
            return e;
        }
    }
}
