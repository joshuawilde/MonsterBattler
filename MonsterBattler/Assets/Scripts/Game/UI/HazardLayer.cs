using System.Collections.Generic;
using UnityEngine;

namespace MonsterBattler.Game.UI
{
    /// <summary>
    /// Persistent entry-hazard indicators (Stealth Rock / Spikes / Toxic Spikes / Sticky Web) as
    /// small world sprites clustered on the ground in front of each side's mon. Driven by the
    /// battle log's |-sidestart|/|-sideend| lines via <see cref="Set"/>/<see cref="Remove"/>.
    /// Template + sprites + anchors are scene/editor-wired; playback only instantiates.
    /// </summary>
    public sealed class HazardLayer : MonoBehaviour
    {
        [SerializeField] SpriteRenderer _template;   // inactive in-scene template child
        [SerializeField] Transform _anchor0;         // ground spot in front of the player mon
        [SerializeField] Transform _anchor1;         // ground spot in front of the enemy mon

        [Header("Hazard sprites (editor-wired assets)")]
        [SerializeField] Sprite _rock;
        [SerializeField] Sprite _spike;
        [SerializeField] Sprite _web;

        static readonly Color ToxicTint = new(0.72f, 0.32f, 0.86f);
        static readonly Color WebTint = new(1f, 1f, 1f, 0.75f);

        readonly Dictionary<string, int>[] _layers = { new(), new() };
        readonly List<SpriteRenderer>[] _spawned = { new(), new() };

        /// <summary>One |-sidestart| line = one layer (the sim already caps stacking).</summary>
        public void Stack(int side, string id)
        {
            if (!IsHazard(id) || side < 0 || side > 1) return;
            _layers[side].TryGetValue(id, out int n);
            _layers[side][id] = n + 1;
            Rebuild(side);
        }

        public void Remove(int side, string id)
        {
            if (side < 0 || side > 1 || !_layers[side].Remove(id)) return;
            Rebuild(side);
        }

        public void ClearAll()
        {
            for (int s = 0; s < 2; s++) { _layers[s].Clear(); Rebuild(s); }
        }

        public static bool IsHazard(string id) =>
            id == "stealthrock" || id == "spikes" || id == "toxicspikes" || id == "stickyweb";

        void Rebuild(int side)
        {
            foreach (var sr in _spawned[side]) if (sr != null) Destroy(sr.gameObject);
            _spawned[side].Clear();

            var anchor = side == 0 ? _anchor0 : _anchor1;
            if (anchor == null || _template == null) return;

            foreach (var kv in _layers[side])
            {
                switch (kv.Key)
                {
                    case "stealthrock": // floating ring of pointed stones
                        Spawn(side, anchor, _rock, new Vector3(-0.25f, 0.45f, 0f), 0.3f, Color.white);
                        Spawn(side, anchor, _rock, new Vector3(0.06f, 0.65f, 0.1f), 0.24f, Color.white);
                        Spawn(side, anchor, _rock, new Vector3(0.3f, 0.4f, -0.05f), 0.27f, Color.white);
                        break;
                    case "spikes": // caltrops on the ground, one per layer
                        for (int i = 0; i < Mathf.Min(kv.Value, 3); i++)
                            Spawn(side, anchor, _spike, SpikeSpot(i), 0.22f, Color.white);
                        break;
                    case "toxicspikes": // purple caltrops, slightly behind the normal ones
                        for (int i = 0; i < Mathf.Min(kv.Value, 2); i++)
                            Spawn(side, anchor, _spike, SpikeSpot(i) + new Vector3(0.12f, 0f, 0.25f), 0.22f, ToxicTint);
                        break;
                    case "stickyweb":
                        Spawn(side, anchor, _web, new Vector3(0f, 0.12f, 0.12f), 0.6f, WebTint);
                        break;
                }
            }
        }

        static Vector3 SpikeSpot(int i) => i switch
        {
            0 => new Vector3(-0.22f, 0.08f, 0f),
            1 => new Vector3(0.08f, 0.06f, 0.16f),
            _ => new Vector3(0.3f, 0.09f, -0.1f),
        };

        void Spawn(int side, Transform anchor, Sprite sprite, Vector3 offset, float scale, Color tint)
        {
            if (sprite == null) return;
            var sr = Instantiate(_template, anchor.position + offset, Quaternion.identity, transform);
            sr.gameObject.SetActive(true);
            sr.sprite = sprite;
            sr.color = tint;
            sr.sortingOrder = 5; // above the ground, below move fx (50)
            sr.transform.localScale = Vector3.one * scale;
            _spawned[side].Add(sr);
        }
    }
}
