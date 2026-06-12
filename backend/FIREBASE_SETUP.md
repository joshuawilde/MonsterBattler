# Firebase setup (manual steps)

These are the parts only the project owner can do. Until they're done, everything runs in
dev-bypass mode (backend: `AUTH_DEV_BYPASS=1`; client: device-id identity) — full flows work
locally, just without real sign-in or push.

## 1. Project

- console.firebase.google.com → create project (e.g. `monsterbattler`). No Analytics needed.

## 2. Auth providers

- Authentication → Sign-in method → enable **Google** and **Apple**.
- Apple also needs the Sign in with Apple capability on your Apple Developer app id +
  a Services ID; Firebase's Apple-provider page walks through it.

## 3. Apps + config files

- Add an iOS app (bundle id) → download `GoogleService-Info.plist` → `MonsterBattler/Assets/`.
- Add an Android app (package name) → download `google-services.json` → `MonsterBattler/Assets/`.

## 4. Unity SDKs

FireFront already has the tgz approach: `com.google.firebase.app/auth/messaging` tarballs in
`Packages/` referenced from `manifest.json` (FireFront pins 11.6.0). Copy the same pattern:
download the Firebase Unity SDK, drop the `.tgz` files in `MonsterBattler/Packages/`, add
`file:` entries to the manifest for **app**, **auth**, **messaging**.

Then swap the dev identity in one place — `BackendApi.TokenProvider`:
```csharp
// after FirebaseAuth sign-in:
BackendApi.SetFirebaseUid(user.UserId);
BackendApi.TokenProvider = () => user.TokenAsync(false).Result; // or cache it
```

## 5. Backend credentials

- Project settings → Service accounts → **Generate new private key** → save as
  `firebase-sa.json` on the backend host; set `GOOGLE_APPLICATION_CREDENTIALS` to its path.
- Remove `AUTH_DEV_BYPASS`. Push (FCM) starts working automatically — the backend already
  sends on friend events via the Admin SDK; no extra FCM setup beyond the service account.

## 6. Wire-up checklist

- Backend deployed with `INTERNAL_API_KEY` set.
- Rivet actor env: `BACKEND_URL`, `INTERNAL_API_KEY` (battle server reports match results).
- Client: `BackendApi.BaseUrl` → your backend URL.
- App start: sign in → `BackendApi.SyncProfile(username)` → register FCM token via
  `BackendApi.RegisterDevice(token)` (hook Firebase Messaging's TokenReceived event).
