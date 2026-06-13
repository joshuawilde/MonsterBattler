using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MonsterBattler.Game
{
    /// <summary>
    /// Heartbeats the backend so the player counts as online — ALWAYS, whether they're on the
    /// menu or in a match. Lives on a persistent object (not the menu), so battle-flow coroutines
    /// (StopAllCoroutines on MenuController) can't kill it. Exposes the live count; the home
    /// screen's "N online" label subscribes.
    /// </summary>
    public sealed class OnlinePresence : MonoBehaviour
    {
        const float PingEvery = 15f;

        public static int Count { get; private set; }
        public static event System.Action<int> CountChanged;

        bool _authed;

        void OnEnable() => FirebaseBootstrap.ProfileSynced += OnAuthed;
        void OnDisable() => FirebaseBootstrap.ProfileSynced -= OnAuthed;
        void OnAuthed(string username, int elo) => _authed = true;

        IEnumerator Start()
        {
            // Wait for a real token before pinging — otherwise the first pings carry the dev
            // placeholder token and the backend (correctly) 401s. ProfileSynced means we're in.
            while (!(_authed && BackendApi.Configured)) yield return null;

            var wait = new WaitForSeconds(PingEvery);
            while (true)
            {
                yield return BackendApi.Online(r =>
                {
                    int n = r != null && r["count"] != null ? (int)r["count"] : Count;
                    if (n != Count) { Count = n; CountChanged?.Invoke(n); }
                });
                yield return wait;
            }
        }
    }
}
