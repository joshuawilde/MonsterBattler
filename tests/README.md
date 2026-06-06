# Sim tests

Standalone xUnit tests for `MonsterBattler.Sim`. They run **outside Unity** via `dotnet test` —
fast (~0.1s) and CLI-driven — which is possible because the Sim asmdef is pure C# with
`noEngineReferences`. The test project compile-links the real Sim sources (no copy, no mock), so
tests exercise exactly the code Unity ships.

## Run

```sh
cd tests/MonsterBattler.Sim.Tests
dotnet test
```

## How it's wired

- `MonsterBattler.Sim.Tests.csproj` globs `../../MonsterBattler/Assets/Scripts/Sim/**/*.cs` into the
  test assembly.
- `TestData.cs` loads the real `StreamingAssets/dex/*.json` (the same data the game uses) via
  System.Text.Json, mirroring the Unity-side `DexLoader`/`RandbatsLoader`. Located via
  `[CallerFilePath]`, so it works regardless of the working directory.
- `TestBattlers.cs` builds battlers/battles against the real Dex.

## Coverage so far

- `DexTests` — dex loads (>800 species etc.), known-species data, randbats ↔ dex referential integrity.
- `RandomTeamGeneratorTests` — 6 distinct valid battlers, determinism per seed, special-attacker Atk-EV trim.
- `ParadoxBoostTests` — Protosynthesis / Quark Drive / Booster Energy (highest-stat ×1.3, Speed ×1.5,
  no-activation guard, lapse when sun ends).
- `ItemEffectTests` — Assault Vest (SpD ×1.5 + blocks status moves), Heavy-Duty Boots (negates Stealth Rock).

Add a test file per mechanic as effects are added — this is the fastest way to assert a new
ability/item/move actually fires.
