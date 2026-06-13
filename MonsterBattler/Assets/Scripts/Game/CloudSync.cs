using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MonsterBattler.Game
{
    /// <summary>
    /// Syncs the local collection (MetaGame save blob) to the backend so it survives reinstalls
    /// and follows the player across devices once they sign in with Apple/Google. Last-write-wins
    /// by a monotonic rev: on login, the newer of cloud/local wins; thereafter every local save
    /// is pushed up (debounced). The server-authoritative username/Elo override whatever's in a
    /// loaded cloud blob. Does nothing until the backend is configured + a profile sync resolves.
    /// </summary>
    public sealed class CloudSync : MonoBehaviour
    {
        bool _ready;          // initial pull/merge done — safe to push
        bool _dirty;          // a local save happened; push after the debounce
        float _dirtyAt;
        const float Debounce = 4f;

        void OnEnable()
        {
            FirebaseBootstrap.ProfileSynced += OnProfileSynced;
            Meta.MetaGame.Saved += OnLocalSaved;
        }

        void OnDisable()
        {
            FirebaseBootstrap.ProfileSynced -= OnProfileSynced;
            Meta.MetaGame.Saved -= OnLocalSaved;
            if (_dirty) StartCoroutine(Push()); // best-effort flush
        }

        void OnProfileSynced(string username, int elo) => StartCoroutine(InitialSync(username, elo));

        IEnumerator InitialSync(string username, int elo)
        {
            JObject cloud = null;
            yield return BackendApi.GetSave(r => cloud = r);
            int cloudRev = cloud != null && cloud["rev"] != null ? (int)cloud["rev"] : -1;
            string cloudData = cloud?["data"]?.ToString();

            var (localRev, localJson) = Meta.MetaGame.Snapshot();
            if (cloudRev > localRev && !string.IsNullOrEmpty(cloudData))
            {
                // Cloud is newer (e.g. fresh device, or this account played elsewhere).
                Meta.MetaGame.ApplyCloud(cloudData, username, elo);
                FindAnyObjectByType<Meta.MenuController>()?.OnCloudRestored();
            }
            else if (localRev > cloudRev)
            {
                yield return BackendApi.PutSave(localRev, localJson); // local is ahead — back it up
            }
            _ready = true;
        }

        void OnLocalSaved()
        {
            if (!_ready) return;
            _dirty = true;
            _dirtyAt = Time.unscaledTime;
        }

        void Update()
        {
            if (_dirty && Time.unscaledTime - _dirtyAt >= Debounce)
            {
                _dirty = false;
                StartCoroutine(Push());
            }
        }

        IEnumerator Push()
        {
            var (rev, json) = Meta.MetaGame.Snapshot();
            yield return BackendApi.PutSave(rev, json);
        }
    }
}
