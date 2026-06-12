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

## Tests

`go test ./...` — in-process integration tests over the real HTTP surface + real sqlite
(profile/duplicate-name, auth rejection, full friend lifecycle, Elo math + leaderboard,
API-key gate). CI runs them on every push.

## Deploy

Two paths, same steps (test → image → push to GHCR → restart):

- **CI (source of truth)**: `.github/workflows/backend.yml` runs on every push to `main`
  touching `backend/`. Tests + builds + pushes `ghcr.io/joshuawilde/monsterbattler-backend`.
  To enable the auto-deploy job: set repo variable `DEPLOY_ENABLED=true` and secrets
  `DEPLOY_SSH_HOST` / `DEPLOY_SSH_USER` / `DEPLOY_SSH_KEY`.
- **Instant, from your machine**: `DEPLOY_HOST=user@vps tools/deploy-backend.sh`
  (needs a one-time `docker login ghcr.io`).

Host setup is the `compose.yml` header — a $5 VPS with docker is enough; SQLite lives on a
volume. Cloud Run works too (set min-instances=1 if you want literally-zero cold start,
though the Go image cold-starts in ~100-300ms anyway); swap to Postgres if scale demands.
