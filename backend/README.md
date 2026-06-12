# MonsterBattler backend

Go service: profiles, Elo leaderboard, friends, FCM push-device registry. Auth is
**Firebase ID tokens** (Apple/Google sign-in happens client-side via Firebase Auth; this
service only verifies). Match results come from the **battle server** (Rivet actor) through
an API-key endpoint, so clients can never forge Elo.

## Run locally (no Firebase project needed)

```sh
cd backend
AUTH_DEV_BYPASS=1 INTERNAL_API_KEY=testkey go run .
# auth with:  Authorization: Bearer dev:<any-uid>
```

## Env

| var | meaning |
|---|---|
| `PORT` | listen port (default 8080) |
| `DB_PATH` | sqlite file (default ./monsterbattler.db) |
| `GOOGLE_APPLICATION_CREDENTIALS` | Firebase service-account JSON (prod) |
| `AUTH_DEV_BYPASS` | `1` = accept `Bearer dev:<uid>`, disable push (local dev) |
| `INTERNAL_API_KEY` | shared secret the battle server sends as `X-Api-Key` |

## Endpoints

All `/v1/*` (except internal) require `Authorization: Bearer <firebase-id-token>`.

- `POST /v1/profile/sync {username}` → `{uid, username, elo, rank}` (creates on first call; 409 if name taken)
- `GET /v1/leaderboard?limit=50` → `{top:[{...rank}], me:{...}}`
- `GET /v1/friends` → `{friends:[{uid,username,elo,status,direction}]}` (pending shows incoming/outgoing)
- `POST /v1/friends/request {username}` (pushes "Friend request" to the target's devices)
- `POST /v1/friends/respond {uid, accept}` (accept only valid for the non-requester)
- `DELETE /v1/friends/{uid}`
- `POST /v1/devices {token, platform}` — register the FCM device token
- `POST /v1/internal/match-result {uid0, uid1, winnerSide}` + `X-Api-Key` → `{elo0, elo1}`
  (winnerSide 0/1/-1-draw; K=32 Elo computed server-side)
- `GET /healthz`

## Deploy

Anything that runs a container: Cloud Run (set min-instances=1 if you want zero cold start,
though a Go scratch image cold-starts in ~100-300ms anyway) or a $5 VPS. SQLite + a volume is
plenty at this scale; swap `store.go` to Postgres if/when it isn't.
