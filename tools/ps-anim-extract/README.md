# ps-anim-extract

Converts Pokémon Showdown's client move animations (`battle-animations-moves.ts`,
the `BattleMoveAnims` table) into a data file (`anims.json`) by *executing* every
anim function against mock scene/sprite objects and recording every call.

## Re-running

```sh
node extract.mjs
```

No dependencies (uses Node's built-in TypeScript type stripping; needs Node >= 22.13,
tested on 24.4). Sources are read from `/tmp/ps-anims-moves.ts` and `/tmp/ps-anims.ts`
and auto-downloaded from the smogon/pokemon-showdown-client master branch if missing.

Outputs:
- `anims.json` — the extracted animation data (minified, ~2 MB)
- `summary.json` — exact/inexact counts, inexact move list, sprite/ease/fade usage tables, errors

## How it works

1. Types are stripped with `node:module`'s `stripTypeScriptTypes`, imports/exports
   removed, and the table evaluated in a `node:vm` context. The `BattleOtherAnims`
   table (shared anims like `contactattack`, `dance`) is extracted from
   `battle-animations.ts` and evaluated into the same context first.
2. Mock `scene` (`showEffect`, `animateEffect`, `backgroundEffect`, `wait`,
   `timeOffset`) and mock attacker/defender sprites (`x/y/z`, `sp`, `behind`,
   `leftof`, `behindx/behindy`, `anim`, `delay`, `isFrontSprite`,
   `isMissedPokemon`) record every call. `Math.random` is a seeded mulberry32
   PRNG reset before every run so runs are bit-identical.
3. Each anim runs 4 times with different attacker/defender anchor coordinates.
   Every numeric scalar is solved as an affine function of the anchors of the
   same axis: `v = ca*A + cd*D + k` (x fields against attacker.x/defender.x,
   y against y, z against z; non-positional fields like scale/opacity/time are
   solved against the x anchors and normally come out constant). Runs 1–3 solve
   the 3x3 system; run 4 verifies. Any scalar off by > 0.01 marks the move
   `"exact": false` and that scalar falls back to the run-1 raw value (`{"k": v}`).

## JSON schema

```jsonc
{
  "<moveid>": {
    "exact": true,            // false => contains non-affine math (sin/cos, cross-axis like y: defender.x)
    "steps": [ ... ],         // the main anim
    "prepare": [ ... ],       // optional: prepareAnim (two-turn moves like solarbeam)
    "residual": [ ... ],      // optional: residualAnim (e.g. partial trapping)
    // or, if evaluation threw:
    "error": "message"
  }
}
```

Step types:

```jsonc
// scene.showEffect(sprite, from, to, ease, fade?, css?)
{ "type": "effect", "sprite": "wisp",          // or "attacker.sp" / "defender.sp" / {url,w,h} inline sprite
  "from": { "x": {"cd":1}, "y": {"cd":1,"k":150}, "z": {"cd":1},
            "scale": {"k":1}, "opacity": {"k":0.4}, "time": {"k":0} },
  "to":   { ... },                             // fully merged end state (from + overrides), per PS semantics
  "ease": "linear",                            // optional transition
  "fade": "fade",                              // optional: "fade" | "explode" | "gone"
  "css":  { "filter": "..." },                 // optional additionalCss passthrough
  "chain": 3 }                                 // optional: continues the DOM element of steps[3] (scene.animateEffect)

// scene.backgroundEffect(color, duration, opacity, delay) — plain numbers, always constants
{ "type": "bg", "color": "#000000", "duration": 600, "opacity": 0.2, "delay": 0 }

// attackerOrDefender.anim(end, ease?) — the mon lunge/return moves
{ "type": "monAnim", "who": "attacker",
  "to": { "x": {"ca":1}, "y": {"ca":1,"k":-10}, "z": {"ca":1},
          "scale": {"k":1}, "opacity": {"k":1}, "time": {"k":75} },
  "ease": "accel" }                            // optional

// attackerOrDefender.delay(ms)
{ "type": "monDelay", "who": "defender", "time": {"k": 260} }
```

Scalar coefficient objects: `{"ca": ..., "cd": ..., "k": ...}` with zero keys
omitted (missing key = 0). Evaluate as `ca * attackerAxisValue + cd * defenderAxisValue + k`
using the axis matching the field name (`x`,`y`,`z`; anything else uses x — by
construction those have ca=cd=0 unless the source mixed axes).

Normalization applied at record time (mirrors `BattleScene#animateEffect`):
`from.time` defaults to 0, `to.time` to `from.time + 500`, both get
`scene.timeOffset` added (from `scene.wait()`); `scale/xscale/yscale` propagate
from `from` to `to`; `to` is the full merge `{...from, ...to}`. `monAnim.to`
defaults to `{x,y,z: sprite pos, scale:1, opacity:1, time:500}` per `Sprite#anim`.

## Known limitations

- `scene.$bg.animate(...)` viewport-shake chains (earthquake, fissure, etc.) are
  stubbed and not recorded — they animate the arena DOM element, not an effect.
- `scene.battle.mySide` (used only by hail) is treated as the origin, so those
  positions appear as constants relative to the player side's base position.
- Attacker is assumed to be the back (player) sprite and defender the front
  sprite; this fixes the signs of `behind()/leftof()/behindx()/behindy()`.
- `Math.random()` values are folded into the constants (one deterministic
  sample per call site, seed 0xC0FFEE) — randomized positions are a single
  representative sample, not a distribution.
- The 31 inexact moves all stem from genuine non-affine source math, e.g.
  `BattleOtherAnims.fastattack` uses `y: defender.x` and solarbeam computes
  `y` from `defender.x - attacker.x`. For those scalars run-1 raw values are
  emitted as constants.
