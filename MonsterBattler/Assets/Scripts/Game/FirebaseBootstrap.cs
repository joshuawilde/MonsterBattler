using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Messaging;
using UnityEngine;

namespace MonsterBattler.Game
{
    /// <summary>
    /// Initializes Firebase, signs in, and points <see cref="BackendApi"/> at the real identity:
    /// anonymous auth for now (enable the Anonymous provider in the Firebase console) — the
    /// account can be upgraded in place to Apple/Google later via LinkWithCredential, keeping
    /// the same uid/Elo. FCM device tokens register with the backend as they arrive.
    /// Falls back silently to dev-bypass identity when Firebase isn't configured.
    /// </summary>
    public sealed class FirebaseBootstrap : MonoBehaviour
    {
        public static FirebaseBootstrap Instance { get; private set; }

        FirebaseAuth _auth;
        string _cachedToken;
        float _tokenFetchedAt = -3600f;

        void Awake() => Instance = this;

        /// <summary>Human-readable account state for the Account panel.</summary>
        public string AccountLabel()
        {
            var u = _auth?.CurrentUser;
            if (u == null) return "Offline — local identity";
            if (u.IsAnonymous) return "Guest account\n<size=70%>Your collection is backed up, but tied to this device.\nSign in with Apple or Google to keep it if you\nreinstall or switch devices.</size>";
            string who = !string.IsNullOrEmpty(u.DisplayName) ? u.DisplayName
                       : !string.IsNullOrEmpty(u.Email) ? u.Email : u.UserId.Substring(0, 8);
            return $"Signed in as {who}";
        }

        public bool IsAnonymous => _auth?.CurrentUser?.IsAnonymous ?? true;

        /// <summary>Link the current (anonymous) account to Apple/Google — keeps the uid, so Elo
        /// and friends survive. If that provider identity already owns another account, signs
        /// into it instead (uid switches; backend profile follows on next sync).
        /// providerId: "apple.com" or "google.com". Device builds only (OS auth sheet).</summary>
        public void LinkWith(string providerId, System.Action<string> status)
        {
            var u = _auth?.CurrentUser;
            if (u == null) { status?.Invoke("Not connected to Firebase"); return; }
            if (Application.isEditor)
            {
                status?.Invoke("Sign-in opens the system sheet on iOS/Android builds");
                return;
            }
            var provider = new FederatedOAuthProvider();
            provider.SetProviderData(new FederatedOAuthProviderData { ProviderId = providerId });
            status?.Invoke("Opening sign-in…");
            u.LinkWithProviderAsync(provider).ContinueWithOnMainThread(t =>
            {
                if (!t.IsFaulted && !t.IsCanceled)
                {
                    OnSignedIn(t.Result.User);
                    status?.Invoke("Account linked!");
                    return;
                }
                // Provider identity already has an account → sign into that one instead.
                _auth.SignInWithProviderAsync(provider).ContinueWithOnMainThread(t2 =>
                {
                    if (t2.IsFaulted || t2.IsCanceled)
                    {
                        status?.Invoke("Sign-in didn't complete");
                        return;
                    }
                    OnSignedIn(t2.Result.User);
                    status?.Invoke("Signed in!");
                });
            });
        }

        void Start()
        {
            // The dedicated server has no player identity and shouldn't load the native SDK.
            if (Application.isBatchMode) return;
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(t =>
            {
                if (t.Result != DependencyStatus.Available)
                {
                    Debug.LogWarning($"[Firebase] unavailable ({t.Result}) — staying on dev identity");
                    SyncProfile(); // dev-bypass backend still gets the dev identity
                    return;
                }
                _auth = FirebaseAuth.DefaultInstance;
                SignIn();
                FirebaseMessaging.TokenReceived += OnFcmToken;
            });
        }

        void SignIn()
        {
            if (_auth.CurrentUser != null) { OnSignedIn(_auth.CurrentUser); return; }
            _auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(t =>
            {
                if (t.IsFaulted || t.IsCanceled)
                {
                    Debug.LogWarning($"[Firebase] anonymous sign-in failed (provider enabled in console?): {t.Exception?.GetBaseException().Message}");
                    return;
                }
                OnSignedIn(t.Result.User);
            });
        }

        void OnSignedIn(FirebaseUser user)
        {
            Debug.Log($"[Firebase] signed in as {user.UserId}");
            BackendApi.SetFirebaseUid(user.UserId);
            BackendApi.TokenProvider = () => FreshToken(user);
            // fetch the first token, THEN do the initial backend sync (ordering matters:
            // a sync before this point would carry a stale/dev token).
            user.TokenAsync(false).ContinueWithOnMainThread(t =>
            {
                if (t.IsFaulted) { Debug.LogWarning($"[Firebase] token fetch failed: {t.Exception?.GetBaseException().Message}"); return; }
                _cachedToken = t.Result;
                _tokenFetchedAt = Time.realtimeSinceStartup;
                SyncProfile();
            });
        }

        /// <summary>Raised after the initial profile sync with the server-authoritative
        /// username + Elo. CloudSync uses it to pull/merge the collection blob.</summary>
        public static event System.Action<string /*username*/, int /*elo*/> ProfileSynced;

        void SyncProfile()
        {
            if (!BackendApi.Configured) return;
            StartCoroutine(BackendApi.SyncProfile(Meta.MetaGame.Profile.username, p =>
            {
                if (p == null) return;
                Debug.Log($"[Backend] profile synced: {p.ToString(Newtonsoft.Json.Formatting.None)}");
                ProfileSynced?.Invoke((string)p["username"], p["elo"] != null ? (int)p["elo"] : 0);
            }));
        }

        // TokenProvider is synchronous; serve the cached token and refresh it in the background
        // when it's older than 30 min (Firebase ID tokens live 60).
        string FreshToken(FirebaseUser user)
        {
            if (Time.realtimeSinceStartup - _tokenFetchedAt > 1800f)
            {
                _tokenFetchedAt = Time.realtimeSinceStartup; // gate concurrent refreshes
                user.TokenAsync(false).ContinueWithOnMainThread(t =>
                {
                    if (!t.IsFaulted) _cachedToken = t.Result;
                });
            }
            return _cachedToken ?? "pending";
        }

        void OnFcmToken(object sender, TokenReceivedEventArgs e)
        {
            Debug.Log("[Firebase] FCM token received");
            StartCoroutine(BackendApi.RegisterDevice(e.Token));
        }

        void OnDestroy()
        {
            if (_auth != null) FirebaseMessaging.TokenReceived -= OnFcmToken;
        }
    }
}
