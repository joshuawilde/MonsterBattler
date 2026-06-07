# Pokémon Showdown UI reference

Target look for MonsterBattler's battle UI. Screenshots captured 2026-06-06.

## showdown-move-buttons.png — move buttons
Clean, **no descriptions on the button**:
- Rounded rectangle, light **type-tinted** background, subtle border.
- Move **name**: large, bold, dark, centered (upper-middle).
- **Type** name: bottom-left, small.
- **PP** (e.g. "8/8"): bottom-right, small, tinted (greenish when full, red when low).
- Terastallize: a checkbox + label in its own bordered box, with a small "FIGHT" pill.

## showdown-info-panel.png — monster info panel (the big win to copy)
White panel, dark text, uses **colored type badges**, not plain text:
- Header: **Name** + gender symbol + **Lxx** (bold).
- Row of **type badges** (e.g. red "FIRE", brown "FIGHT").
- Stat line: `HP:110  Atk:123  Def:65  SpA:100  SpD:65  Spe:65`.
- Effectiveness rows, each a multiplier label followed by **type badges**:
  `x2:  [GROUND][WATER][FLYING][PSYCHIC]` · `x0.5: [STEEL][FIRE][GRASS][ICE][DARK]` · `x0.25: [BUG]`
- `HP: 100%`
- `Possible abilities: Blaze, Reckless`
- `Spe 114 or 157 (before external modifiers)`

## showdown-move-tooltip.png — move detail (hover/tap tooltip, separate from the button)
- Move **name** (bold) + type badge + category icon (physical/special/status).
- `Base power: 110.5 (1.3× from Tough Claws)`
- `Accuracy: 100%`
- Description paragraph.
- Flags: `✓ Contact (triggers Iron Barbs, Spiky Shield, etc)`
- `◉ Super effective vs. Emboar (2×)` — contextual effectiveness vs the current foe.

## showdown-floating-text.png — floating combat text
When something happens to a mon, a small chip floats up from its sprite and fades out:
- `+11%` (green) on heal, `-X%` (red) on damage, stat arrows on boosts/drops, status, "Miss"/"Failed".
- Ability procs show the ability name in a chip ("Poison Heal", blue).
Implement as a pooled/instantiated chip that lerps up ~80px and fades over ~0.9s, spawned over the
active mon's slot during turn playback.

## showdown-full-battle.png — overall layout
Field at top (sprites + HP bars + 6-ball team rosters per side), then the action area:
"What will X do?" + Attack (4 move buttons) + Terastallize + Switch (6 portrait buttons).

## Key takeaways for our UI
- **Type badges** everywhere (types, effectiveness) — small colored chips, not "Fire ×2" text.
- Move buttons stay **clean** (name/type/PP). Move details belong in a tooltip/panel, not on the button.
- Rounded, lightly-tinted panels; dark text on light backgrounds.
