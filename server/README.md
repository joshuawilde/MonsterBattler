# Dedicated battle server (Rivet)

The server is the same Unity build running headless: `NetBootstrap` sees batchmode and starts a
FishNet server; `NetBattleManager` pairs the first two teams, runs the authoritative sim, and
relays inputs (clients run deterministic mirrors — see `Assets/Scripts/Game/Net/`).

## Pipeline

```sh
tools/deploy-server.sh -e prod      # build (editor bridge) → docker image → rivet deploy
```

Pieces, individually:
1. `python3 tools/build-server.py` — Linux dedicated-server build via the open editor's
   `build.server` bridge command → `MonsterBattler/Builds/LinuxServer/` (~92 MB, Mono).
2. `docker buildx build --platform linux/amd64 -f server/Dockerfile -t monsterbattler-server .`
3. `rivet deploy` — uses `rivet.json` at the repo root (build name `game`).

## Prereqs

- Unity editor open on the project (the build runs through the MCP bridge).
- Docker running.
- `rivet login` once (interactive browser auth).

## Known notes

- **Local `docker run` crashes on Apple Silicon** with a Mono JIT assertion
  (`x86-codegen.h … offset == (gint32)offset`) — that's the x86 emulation layer, not the build.
  It runs on real x86_64 hosts (Rivet). Verify server logic locally via the editor instead:
  Battle Online with `_hostInEditor` runs the identical server code in-process.
- The server listens on UDP `PORT_game` (Rivet-injected; 7777 default). Clients pass
  `-mphost`/`-mpport`, or wire the Rivet actor/lobby resolution into `NetBootstrap.JoinOnline`.
- TODO once a Rivet project exists: create the environment, decide actor lifecycle
  (one actor per match vs a long-lived lobby server), and point `NetBootstrap` at the
  Rivet endpoint instead of the inspector host/port.
