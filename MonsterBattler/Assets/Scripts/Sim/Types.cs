namespace MonsterBattler.Sim
{
    public enum MonType
    {
        None = 0,
        Normal, Fire, Water, Electric, Grass, Ice, Fighting, Poison,
        Ground, Flying, Psychic, Bug, Rock, Ghost, Dragon, Dark, Steel, Fairy,
        // Stellar is the gen9 Tera-only type
        Stellar,
    }

    public enum MoveCategory { Physical, Special, Status }

    public enum Stat { HP, Atk, Def, SpA, SpD, Spe, Acc, Eva }

    public enum StatusCondition { None, Burn, Freeze, Paralysis, Poison, BadlyPoisoned, Sleep, Frostbite }

    public enum Weather { None, Sun, Rain, Sandstorm, Snow, HarshSun, HeavyRain, StrongWinds }

    public enum Terrain { None, Electric, Grassy, Misty, Psychic }

    public enum Gender { Genderless, Male, Female }
}
