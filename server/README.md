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

## Rivet deploy status (2026-06-12)

- Project: **`monsterbattl-nwo`** (display "MonsterBattler", game_id `7ab7fe31-…`), envs `prod` + `staging`.
- Auth: CLI reads the cloud token from `RIVET_CLOUD_TOKEN` env (in `secrets/rivet.env`, gitignored).
- **Build IS deployed to prod**: `RIVET_CLOUD_TOKEN=… rivet deploy -e prod` builds the amd64 image
  and uploads it. Two gotchas hit + fixed/worked-around:
  1. Rivet rejects root containers → Dockerfile now adds a non-root `USER 10001`.
  2. The deploy's final tag step 500s (Rivet-side), leaving the build untagged. Workaround:
     `rivet build patch-tags <BUILD_ID> -e prod -t name=game,current=true`. Done — the current
     prod build is tagged `name=game, current=true` (what actors reference).
- **BLOCKED — no compute regions provisioned**. Root cause confirmed: the regions API
  (`GET /regions?project=monsterbattl-nwo&environment=prod`) returns `{"regions":[]}`. With no
  regions, `rivet actor create` reports "no regions" (and 500s if a region is forced). The build
  is deployed + current; there's just nowhere to run it. This is a Rivet account/project setup
  item — enable/request compute regions for this project (likely tied to the rivet.gg→rivet.dev
  platform migration and/or billing/plan). Once `regions` is non-empty, `rivet actor create -e prod
  -b name=game -p game=udp:7777 -r <region>` and the backend's actors.create work unchanged.
  (Minor: confirm the CLI port arg format — `game=udp:7777` gave a "missing value for field name"
  validation error; verify against current docs when regions are live.)

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
