# MonsterBattler

Unity 6 mobile-first turn-based monster battler that ports Pokemon Showdown gen9 battle logic
to C#. The sim engine is a pure-C# library (no Unity dependencies), so it's unit-testable in
isolation and the same module could ship to any C# host.

## Layout

```
MonsterBattler/                 Unity project (Unity 6, URP, uGUI, Input System)
  Assets/
    Editor/MCP/                 Editor HTTP bridge for the MCP server
    Scripts/Sim/                Pure-C# battle engine (events, effects, damage calc)
    Scripts/Game/               MonoBehaviours that bridge sim ↔ scene + AI
    Scenes/BattleScene.unity    Sphere placeholders, HP bars, move/switch UI
    StreamingAssets/dex/        Species / moves / abilities / items as JSON
mcp-server/                     Node MCP server that brokers Claude ↔ Unity Editor
.mcp.json                       Claude Code MCP registration
```

## Architecture

**Sim engine.** Event-driven. Every gameplay rule is an `Effect` subclass with `On*` virtual
hooks (`OnBasePower`, `OnTryHit`, `OnResidual`, `OnSwitchIn`, etc.). `Battle.RunX` dispatchers
walk active effects in well-defined order. Abilities, items, statuses, side conditions, field
conditions, and move secondaries all use the same pattern. See `Effects/Abilities/BlazeEffect.cs`
for the canonical short example.

**Unity layer.** All UI is authored in `Assets/Scenes/BattleScene.unity` / prefabs and assigned
via `[SerializeField]` references — `BattleView` doesn't `new GameObject()` anything at runtime.
The one allowed runtime pattern is `Instantiate(prefab)`.

**MCP.** `mcp-server/` runs a stdio MCP server that forwards JSON commands to an HTTP listener
running inside the Unity Editor (`Assets/Editor/MCP/MCPBridge.cs`). Used to let Claude do
scene/prefab/component edits, batch ops, read the console, and drive play mode without ever
clicking the editor.

## Setup

```bash
# Open Unity 6 (6000.0.58f1+) and open the MonsterBattler/ project.
# The editor MCP bridge auto-starts and prints:
#   [MCP] Bridge listening on http://127.0.0.1:17984/

cd mcp-server
npm install        # builds dist/ automatically via the prepare script
```

Restart Claude Code so `.mcp.json` picks up the server. Test from another terminal:

```bash
curl -s -X POST http://127.0.0.1:17984/ \
     -H 'content-type: application/json' \
     -d '{"id":"x","command":"meta.ping","params":{}}'
```

## Status

Gen 9 mechanics done so far:
- Full type chart, STAB, type effectiveness, crits (with crit stages), accuracy/evasion stages
- PP tracking, stat-stage system, status conditions (Burn)
- Event-driven Effect system with auto-discovery
- 6-mon teams, switching, auto-switch on faint, priority + Speed move ordering
- Abilities: Blaze, Levitate, Intimidate, Speed Boost, Static, Flash Fire, Sturdy
- Stat-modifying moves: Swords Dance, Nasty Plot, Calm Mind, Bulk Up, Dragon Dance
- High-crit moves: Slash, Stone Edge
- PS-faithful RandomPlayerAI bot opponent

Still pending: weather + setting abilities, volatiles (Leech Seed, Substitute, Protect, Confusion),
status moves (Toxic, Will-O-Wisp, etc.), items, hazards, recoil/drain, Terastallization, sprites.
