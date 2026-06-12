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
        FirebaseAuth _auth;
        string _cachedToken;
        float _tokenFetchedAt = -3600f;

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

        void SyncProfile()
        {
            if (!BackendApi.Configured) return;
            StartCoroutine(BackendApi.SyncProfile(Meta.MetaGame.Profile.username,
                p => { if (p != null) Debug.Log($"[Backend] profile synced: {p.ToString(Newtonsoft.Json.Formatting.None)}"); }));
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
