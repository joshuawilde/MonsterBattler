using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace MonsterBattler.Game
{
    /// <summary>
    /// Thin client for the Go backend (backend/README.md). Every call sends a Firebase ID token
    /// as the bearer; until the Firebase Auth SDK is wired in, <see cref="TokenProvider"/>
    /// defaults to a dev-bypass token derived from the device id (backend must run with
    /// AUTH_DEV_BYPASS=1). Swap TokenProvider for FirebaseAuth.CurrentUser.TokenAsync later —
    /// nothing else changes.
    /// </summary>
    public static class BackendApi
    {
        /// <summary>Backend base URL; override from a bootstrap or build config.</summary>
        public static string BaseUrl = "http://127.0.0.1:8080";

        /// <summary>True once a bootstrap explicitly configured the backend (calls are pointless before).</summary>
        public static bool Configured;

        /// <summary>Returns the bearer token for requests. Default: dev-bypass device identity.</summary>
        public static System.Func<string> TokenProvider = () => "dev:" + DevUid();

        /// <summary>Stable identity for this player (Firebase uid in prod, device id in dev).</summary>
        public static string Uid => _uidOverride ?? DevUid();
        static string _uidOverride;
        public static void SetFirebaseUid(string uid) => _uidOverride = uid;

        static string DevUid() => "dev-" + SystemInfo.deviceUniqueIdentifier;

        // ---- calls (coroutines; onDone gets the parsed body or null on failure) ----------------

        public static IEnumerator SyncProfile(string username, System.Action<JObject> onDone = null)
            => Post("/v1/profile/sync", new JObject { ["username"] = username }, onDone);

        public static IEnumerator GetLeaderboard(int limit, System.Action<JObject> onDone)
            => Get($"/v1/leaderboard?limit={limit}", onDone);

        public static IEnumerator GetFriends(System.Action<JObject> onDone)
            => Get("/v1/friends", onDone);

        public static IEnumerator SendFriendRequest(string username, System.Action<JObject> onDone = null)
            => Post("/v1/friends/request", new JObject { ["username"] = username }, onDone);

        public static IEnumerator RespondFriend(string uid, bool accept, System.Action<JObject> onDone = null)
            => Post("/v1/friends/respond", new JObject { ["uid"] = uid, ["accept"] = accept }, onDone);

        public static IEnumerator RegisterDevice(string fcmToken, System.Action<JObject> onDone = null)
            => Post("/v1/devices", new JObject { ["token"] = fcmToken, ["platform"] = Application.platform.ToString() }, onDone);

        // ---- plumbing ---------------------------------------------------------------------------

        static IEnumerator Get(string path, System.Action<JObject> onDone)
        {
            using var req = UnityWebRequest.Get(BaseUrl + path);
            yield return Send(req, onDone);
        }

        static IEnumerator Post(string path, JObject body, System.Action<JObject> onDone)
        {
            using var req = new UnityWebRequest(BaseUrl + path, "POST");
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body.ToString()));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return Send(req, onDone);
        }

        static IEnumerator Send(UnityWebRequest req, System.Action<JObject> onDone)
        {
            req.SetRequestHeader("Authorization", "Bearer " + TokenProvider());
            req.timeout = 10;
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Backend] {req.url} failed: {req.responseCode} {req.error} {req.downloadHandler?.text}");
                onDone?.Invoke(null);
                yield break;
            }
            JObject parsed = null;
            try { parsed = JObject.Parse(req.downloadHandler.text); }
            catch { /* non-json body */ }
            onDone?.Invoke(parsed);
        }
    }
}
