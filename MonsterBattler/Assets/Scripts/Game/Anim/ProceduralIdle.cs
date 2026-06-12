using System.Collections.Generic;
using UnityEngine;

namespace MonsterBattler.Game.Anim
{
    /// <summary>
    /// Drop-on, animation-free idle for any rigged humanoid/creature. Auto-discovers bones by name
    /// and adds a gentle layered wobble — a breathing sway driven from the spine upward, light arm/
    /// hand/knee motion — while keeping the pelvis (and therefore the feet) essentially planted.
    ///
    /// It rotates bones in their LOCAL space around their rest pose, so it works regardless of the
    /// rig's axis conventions: each joint gets a small 3-axis Lissajous wobble at slightly different
    /// rates, which reads as "alive" without needing to know which axis is forward/up.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProceduralIdle : MonoBehaviour
    {
        [Header("Feel")]
        [Tooltip("Overall motion multiplier. 1 = tuned default, lower = subtler.")]
        [Range(0f, 3f)] public float amount = 1f;
        [Tooltip("Breathing tempo (cycles ~ this per 6s).")]
        [Range(0.1f, 4f)] public float speed = 1.1f;
        [Tooltip("Animate the legs a little (knee/thigh). Off = feet fully planted.")]
        public bool animateLegs = true;
        [Tooltip("Per-instance phase offset so identical models don't move in lockstep.")]
        public float seed = 0f;

        enum Cat { None, Hips, Spine, Neck, Head, Shoulder, UpperArm, Forearm, Hand, Finger, Thigh, Calf, Tail }

        // Per-category wobble amplitude in degrees (x,y,z) + speed multiplier.
        static readonly Dictionary<Cat, (Vector3 amp, float spd)> Profile = new()
        {
            { Cat.Hips,     (new Vector3(0.30f, 0.30f, 0.40f), 1.0f) }, // tiny — moving this moves the feet
            { Cat.Spine,    (new Vector3(1.8f,  0.9f,  1.2f),  1.0f) }, // the breathing driver (upper body only)
            { Cat.Neck,     (new Vector3(1.2f,  1.0f,  1.2f),  1.0f) },
            { Cat.Head,     (new Vector3(1.6f,  1.4f,  1.5f),  0.95f) },
            { Cat.Shoulder, (new Vector3(1.0f,  0.8f,  1.2f),  1.05f) },
            { Cat.UpperArm, (new Vector3(2.5f,  1.5f,  2.8f),  1.1f) },
            { Cat.Forearm,  (new Vector3(1.8f,  1.0f,  1.4f),  1.15f) },
            { Cat.Hand,     (new Vector3(3.0f,  2.5f,  3.0f),  1.3f) },
            { Cat.Finger,   (new Vector3(4.0f,  3.0f,  4.0f),  1.4f) },
            { Cat.Thigh,    (new Vector3(0.6f,  0.4f,  0.6f),  0.9f) }, // small — keeps feet roughly planted
            { Cat.Calf,     (new Vector3(1.0f,  0.3f,  0.5f),  0.9f) }, // a little knee bend
            { Cat.Tail,     (new Vector3(4.0f,  3.0f,  5.0f),  0.8f) },
        };

        sealed class Joint
        {
            public Transform T;
            public Quaternion Rest;
            public Vector3 Amp;
            public float Spd;
            public float Phase;
        }

        readonly List<Joint> _joints = new();

        void OnEnable() => Discover();

        [ContextMenu("Rediscover bones")]
        public void Discover()
        {
            // Restore any joints we were already driving before rebuilding.
            foreach (var j in _joints) if (j.T != null) j.T.localRotation = j.Rest;
            _joints.Clear();

            foreach (var t in GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (t == transform) continue;
                var cat = Classify(t.name);
                if (cat == Cat.None) continue;
                if (!animateLegs && (cat == Cat.Thigh || cat == Cat.Calf)) continue;
                if (!Profile.TryGetValue(cat, out var p)) continue;

                // Left/right limbs move in opposition; everything gets a stable per-bone offset.
                bool right = t.name.StartsWith("R_") || t.name.StartsWith("r_") || t.name.Contains("Right") || t.name.Contains(".R");
                float phase = (right ? Mathf.PI : 0f) + seed + Hash(t.name) * Mathf.PI * 2f;

                _joints.Add(new Joint { T = t, Rest = t.localRotation, Amp = p.amp, Spd = p.spd, Phase = phase });
            }
            Debug.Log($"[ProceduralIdle] '{name}' driving {_joints.Count} bones");
        }

        void LateUpdate()
        {
            float w = Time.time * speed;
            foreach (var j in _joints)
            {
                if (j.T == null) continue;
                float p = j.Phase;
                // Three slightly-detuned sines → organic, axis-agnostic wobble.
                var e = new Vector3(
                    j.Amp.x * Mathf.Sin(w * j.Spd + p),
                    j.Amp.y * Mathf.Sin(w * j.Spd * 0.8f + p + 1.7f),
                    j.Amp.z * Mathf.Sin(w * j.Spd * 1.3f + p + 3.1f));
                j.T.localRotation = j.Rest * Quaternion.Euler(e * amount);
            }
        }

        void OnDisable()
        {
            foreach (var j in _joints) if (j.T != null) j.T.localRotation = j.Rest;
        }

        // --- bone name → category (specific keywords first) -----------------------------------
        static Cat Classify(string raw)
        {
            string n = raw.ToLowerInvariant();
            // Skip roll-correction twist helpers and rig masters — but NOT a "NeckTwist" (that IS the neck).
            if ((n.Contains("twist") && !n.Contains("neck")) || n.Contains("ik") || n.Contains("root") ||
                n.Contains("armature") || n.Contains("skeleton")) return Cat.None;
            if (n.Contains("toe") || n.Contains("foot") || n.Contains("ankle") || n.Contains("ball")) return Cat.None; // planted
            if (n.Contains("finger") || n.Contains("thumb") || n.Contains("index") ||
                n.Contains("middle") || n.Contains("ring") || n.Contains("pinky")) return Cat.Finger;
            if (n.Contains("hand") || n.Contains("wrist")) return Cat.Hand;
            if (n.Contains("forearm") || n.Contains("lowerarm") || n.Contains("elbow")) return Cat.Forearm;
            if (n.Contains("upperarm") || n.Contains("uparm")) return Cat.UpperArm;
            if (n.Contains("clavicle") || n.Contains("shoulder") || n.Contains("collar")) return Cat.Shoulder;
            if (n.Contains("calf") || n.Contains("shin") || n.Contains("lowerleg") || n.Contains("knee")) return Cat.Calf;
            if (n.Contains("thigh") || n.Contains("upperleg") || n.Contains("upleg")) return Cat.Thigh;
            if (n.Contains("head") || n.Contains("skull") || n.Contains("jaw")) return Cat.Head;
            if (n.Contains("neck")) return Cat.Neck;
            if (n.Contains("tail")) return Cat.Tail;
            if (n.Contains("spine") || n.Contains("chest") || n.Contains("abdomen") ||
                n.Contains("torso") || n.Contains("back") || n.Contains("rib")) return Cat.Spine;
            if (n.Contains("pelvis") || n.Contains("hips") || n.Contains("cog")) return Cat.Hips;
            if (n.Contains("arm")) return Cat.UpperArm; // generic "arm" fallback
            if (n.Contains("leg")) return Cat.Thigh;
            return Cat.None;
        }

        // Deterministic 0..1 from a string, for per-bone phase variety.
        static float Hash(string s)
        {
            unchecked
            {
                uint h = 2166136261u;
                foreach (char c in s) { h ^= c; h *= 16777619u; }
                return (h % 1000u) / 1000f;
            }
        }
    }
}
