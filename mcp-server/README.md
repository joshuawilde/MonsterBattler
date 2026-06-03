# monsterbattler-mcp

In-process Unity MCP for this project. Two halves:

1. **Unity Editor bridge** (`MonsterBattler/Assets/Editor/MCP/`) — C# editor scripts that run an
   HTTP listener inside the running Unity Editor on `127.0.0.1:17984`. Commands execute on the
   main thread via `EditorApplication.update`.
2. **Node MCP server** (this folder) — stdio MCP server that Claude Code talks to. Each tool
   call forwards a JSON command to the Unity bridge.

## Setup

```bash
cd mcp-server
npm install
npm run build
```

The `.mcp.json` at the repo root registers this server with Claude Code automatically — restart
Claude Code after first install so it picks it up.

## Running

1. Open `MonsterBattler/` in Unity 6. On editor load you should see in the Console:
   `[MCP] Bridge listening on http://127.0.0.1:17984/`
2. Verify from outside Unity:
   ```bash
   curl -s -X POST http://127.0.0.1:17984/ \
        -H 'content-type: application/json' \
        -d '{"id":"x","command":"meta.ping","params":{}}'
   ```
3. From Claude: call `unity_ping` or `unity_list_commands`.

## Adding a command

1. Add a handler in `MonsterBattler/Assets/Editor/MCP/Handlers/...` using `MCPCommandRegistry.Register("namespace.verb", p => ...)`.
2. Optionally add a first-class wrapper in `mcp-server/src/index.ts`. If you don't, it's still callable via `unity_call`.

## Menu

`MonsterBattler → MCP → Restart Bridge` / `Status` lets you kick the listener if it dies.
