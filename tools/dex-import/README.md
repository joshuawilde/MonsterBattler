# dex-import

Regenerates `MonsterBattler/Assets/StreamingAssets/dex/{species,moves,abilities,items}.json`
from the gen9 dataset in [`@pkmn/dex`](https://github.com/pkmn/ps), in the exact schema
that `Assets/Scripts/Game/DexLoader.cs` parses.

## Run

```sh
cd tools/dex-import
npm install      # first time only
node convert.mjs
```

## What it does

- Pulls the **gen9-legal** pool (`isNonstandard === null`): ~876 species, ~685 moves,
  ~310 abilities, ~249 items. This is the Showdown random-battle species pool.
- Maps PS fields to ours: `accuracy: true` → `0` (never-miss), `critRatio` shifted so
  `0` = base, `recoil`/`drain` arrays → num/den, `selfdestruct` → `selfKO`,
  `selfSwitch` → `pivotsOut`, `multihit` → min/max, flinch secondary → `flinchChance`,
  `flags.charge` → `twoTurn`, PS target strings → our `MoveTarget` enum names.
- **Preserves hand-authored `effectId`** on moves/abilities/items by merging existing
  JSON on top of the regenerated pure-data. Re-running never clobbers Effect-class links.
- Keeps any existing entry whose id is not in the gen9 set (e.g. Pursuit, King's Shield —
  gen8 moves we implemented effects for).

## Not imported (still TODO)

- Learnsets — needed before the randbats team generator can pick legal movesets.
- Species weight/height/gender, evolutions.
- Per-move secondary status/stat effects beyond flinch (those need Effect classes).
