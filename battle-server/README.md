# MonsterBattler battle server

Pure-dotnet WebSocket server that runs authoritative battles. References the **same pure-C# sim**
the Unity client ships (no Unity, no FishNet) — only inputs cross the wire and both ends run an
identical deterministic mirror. One process hosts thousands of concurrent turn-based matches via
async I/O (idle connections cost no threads); each match has a single-threaded event pump, so the
sim is never touched concurrently.

## Run locally

```sh
cd battle-server
INTERNAL_API_KEY=k BACKEND_URL=http://127.0.0.1:8080 dotnet run -c Release
# health: curl localhost:8081/healthz
```

## Protocol

- Backend registers a paired match first: `POST /internal/match {matchId, uid0, uid1}` + `X-Api-Key`.
- Client connects `ws://host:PORT/ws`, then:
  - `{t:join, matchId, uid, team:<TeamSpec>}` → server replies `{t:start, side, seed, team0, team1}`
    once both joined (or fills the other side with a bot after ~12s).
  - `{t:choice, choice:{kind,moveId,switchToIndex,terastallize,pivotToIndexPlusOne}}`
  - `{t:replace, index}` (forced-switch pick; -1 = none)
  - `{t:chat, text}` → relayed to the opponent as `{t:chat, from, text}`
  - server pushes `{t:turn, s0, s1}`, `{t:replace, p0, p1}`, `{t:abort}`, `{t:error, text}`
- On finish (PvP only, not bot matches) the server reports to the backend:
  `POST $BACKEND_URL/v1/internal/match-result {uid0, uid1, winnerSide}` + key.

## Env

| var | meaning |
|---|---|
| `PORT` | listen port (default 8081) |
| `INTERNAL_API_KEY` | shared secret for `/internal/match` + result reports |
| `BACKEND_URL` | Go backend base URL (Elo reporting); empty = no reporting |
| `DEX_DIR` | dex JSON dir (default `./dex` beside the binary; the Dockerfile copies it there) |

## Deploy

`docker build -f battle-server/Dockerfile -t monsterbattler-battle .` then run on the VPS
(alongside the Go backend) with the env above. UDP isn't needed — WebSocket over TCP, so it
sails through NAT/firewalls. Scale horizontally later by running N instances and having the
backend round-robin matches across them (each match lives entirely on one instance).
