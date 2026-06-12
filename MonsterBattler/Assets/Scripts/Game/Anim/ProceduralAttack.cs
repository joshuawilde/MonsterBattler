using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MonsterBattler.Game.Anim
{
    /// <summary>
    /// Drop-on, animation-free attack for any rigged creature. Calling <see cref="Attack()"/>
    /// runs an anticipate → strike → return cycle: the whole model lurches forward and a
    /// handful of bones tilt in world space (arms swing forward, spine leans, head drops,
    /// tail whips backward) before returning to rest.
    ///
    /// World-space tilts are computed from the model's <c>forward</c> direction so it works
    /// regardless of bone-axis conventions — same robustness trick the idle uses for wobble,
    /// just driven toward a target instead of oscillating.
    ///
    /// While playing, <see cref="ProceduralIdle"/> on the same GameObject is disabled and
    /// restored afterward so they don't fight over the same bones.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProceduralAttack : MonoBehaviour
    {
        [Header("Timing (seconds)")]
        [Range(0f, 0.5f)] public float anticipationDuration = 0.10f;
        [Range(0f, 0.5f)] public float strikeDuration = 0.15f;
        [Range(0f, 0.5f)] public float returnDuration = 0.18f;

        [Header("Lurch")]
        [Tooltip("World-units the model translates forward at peak strike.")]
        public float lurchDistance = 0.4f;
        [Tooltip("Fraction of lurchDistance the model pulls back during anticipation.")]
        [Range(0f, 1f)] public float anticipationPullback = 0.2f;

        [Header("Direction")]
        [Tooltip("If set, the lurch direction is toward this transform's position. Otherwise transform.forward is used.")]
        public Transform target;
        [Tooltip("If non-zero, this world-space vector is used as the lurch direction. Wins over 'target'.")]
        public Vector3 forwardOverride;

        [Header("Pose (degrees of world-space tilt around modelRight)")]
        public float spineLean = 12f;
        public float headDrop = 15f;
        [Tooltip("Mouth-open. Composes on top of headDrop — net jaw-vs-head opening is roughly (jawOpen − headDrop).")]
        public float jawOpen = 45f;
        public float upperArmSwing = 50f;
        public float forearmSwing = 30f;
        public float tailBackswing = -20f;
        [Tooltip("Multiplier on every pose value above. 0 = lurch only, no bone overlay.")]
        [Range(0f, 2f)] public float intensity = 1f;

        bool _playing;

        enum Cat { None, Spine, Head, Jaw, UpperArm, Forearm, Hand, Tail }

        sealed class BoneTarget
        {
            public Transform T;
            public Quaternion Rest;        // local rotation captured at Play() start
            public Quaternion Strike;      // local rotation at peak strike
        }

        readonly List<BoneTarget> _bones = new();

        /// <summary>Fire-and-forget attack. Safe to call again while one is playing — the request is dropped.</summary>
        public Coroutine Attack() => _playing ? null : StartCoroutine(Run());
        public Coroutine Attack(Transform tgt) { target = tgt; return Attack(); }
        public Coroutine Attack(Vector3 worldForward) { forwardOverride = worldForward; return Attack(); }

        [ContextMenu("Attack (test)")] void AttackFromMenu() => Attack();

        IEnumerator Run()
        {
            _playing = true;
            var idle = GetComponent<ProceduralIdle>();
            bool idleWas = idle != null && idle.enabled;
            if (idle != null) idle.enabled = false; // OnDisable restores rest pose so we start clean

            Vector3 fwd = ComputeForward();
            Vector3 modelRight = ComputeRight(fwd);

            Vector3 startPos = transform.position;
            Vector3 backPos = startPos - fwd * (lurchDistance * anticipationPullback);
            Vector3 hitPos = startPos + fwd * lurchDistance;

            BuildPose(modelRight);

            // Anticipation: small pull-back + light pose lead-in (negative blend).
            yield return Drive(startPos, backPos, 0f, -0.25f * intensity, anticipationDuration);

            // Strike: snap to full pose + forward lurch.
            yield return Drive(backPos, hitPos, -0.25f * intensity, 1f * intensity, strikeDuration);

            // Return: lerp back to rest.
            yield return Drive(hitPos, startPos, 1f * intensity, 0f, returnDuration);

            // Snap exactly to rest so subsequent code sees clean state.
            transform.position = startPos;
            foreach (var b in _bones) if (b.T != null) b.T.localRotation = b.Rest;

            if (idle != null && idleWas) idle.enabled = true;
            _playing = false;
        }

        // Lerps position from a→b and bone blend from blendFrom→blendTo (blend ∈ [-1..1], 0 = rest,
        // 1 = full strike pose). Negative blend pulls bones in the opposite direction (anticipation).
        IEnumerator Drive(Vector3 a, Vector3 b, float blendFrom, float blendTo, float duration)
        {
            if (duration <= 0f) { ApplyFrame(b, blendTo); yield break; }
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
                ApplyFrame(Vector3.LerpUnclamped(a, b, u), Mathf.LerpUnclamped(blendFrom, blendTo, u));
                yield return null;
            }
            ApplyFrame(b, blendTo);
        }

        void ApplyFrame(Vector3 pos, float blend)
        {
            transform.position = pos;
            foreach (var b in _bones)
            {
                if (b.T == null) continue;
                // Slerp around Rest by 'blend' fraction toward Strike. Negative blend extrapolates
                // (anticipation lead-in) and Quaternion.SlerpUnclamped handles that cleanly.
                b.T.localRotation = Quaternion.SlerpUnclamped(b.Rest, b.Strike, blend);
            }
        }

        Vector3 ComputeForward()
        {
            if (forwardOverride.sqrMagnitude > 0.0001f) return forwardOverride.normalized;
            if (target != null)
            {
                var d = target.position - transform.position;
                d.y = 0f;
                if (d.sqrMagnitude > 0.0001f) return d.normalized;
            }
            return transform.forward;
        }

        static Vector3 ComputeRight(Vector3 fwd) => Vector3.Cross(Vector3.up, fwd).normalized;

        // Bone classification + pose target build. World tilt → local target rotation:
        //   targetWorld  = AngleAxis(deg, worldAxis) * boneWorldAtRest
        //   targetLocal  = inverse(parentWorld) * targetWorld
        void BuildPose(Vector3 worldRight)
        {
            _bones.Clear();
            foreach (var t in GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (t == transform) continue;
                var cat = Classify(t.name);
                if (cat == Cat.None) continue;

                float deg = cat switch
                {
                    Cat.Spine    => spineLean,
                    Cat.Head     => headDrop,
                    Cat.Jaw      => jawOpen,
                    Cat.UpperArm => upperArmSwing,
                    Cat.Forearm  => forearmSwing,
                    Cat.Hand     => forearmSwing * 0.6f,
                    Cat.Tail     => tailBackswing,
                    _ => 0f,
                };
                if (Mathf.Approximately(deg, 0f)) continue;

                var rest = t.localRotation;
                // Capture current world rotation (right now equals rest-world since we just
                // disabled idle and it restored the pose in OnDisable).
                var worldDelta = Quaternion.AngleAxis(deg, worldRight);
                var targetWorld = worldDelta * t.rotation;
                var parentWorld = t.parent != null ? t.parent.rotation : Quaternion.identity;
                var targetLocal = Quaternion.Inverse(parentWorld) * targetWorld;

                _bones.Add(new BoneTarget { T = t, Rest = rest, Strike = targetLocal });
            }
        }

        // Narrow classifier: only the categories we actually pose for attack.
        static Cat Classify(string raw)
        {
            string n = raw.ToLowerInvariant();
            if (n.Contains("twist") || n.Contains("ik") || n.Contains("armature") ||
                n.Contains("foot") || n.Contains("toe") || n.Contains("finger")) return Cat.None;
            if (n.Contains("hand") || n.Contains("wrist")) return Cat.Hand;
            if (n.Contains("forearm") || n.Contains("lowerarm") || n.Contains("elbow")) return Cat.Forearm;
            if (n.Contains("upperarm") || n.Contains("uparm")) return Cat.UpperArm;
            // Jaw must be tested BEFORE head — otherwise "jaw" would match the head branch.
            if (n.Contains("jaw") || n.Contains("mandible") || n.Contains("mouth")) return Cat.Jaw;
            if (n.Contains("head") || n.Contains("skull")) return Cat.Head;
            if (n.Contains("tail")) return Cat.Tail;
            if (n.Contains("spine") || n.Contains("chest")) return Cat.Spine;
            // Generic "arm" only — exclude "Armature" via the earlier check.
            if (n.Contains("arm")) return Cat.UpperArm;
            return Cat.None;
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            // Show forward arrow so you can see which way the lurch will go.
            Gizmos.color = Color.cyan;
            Vector3 fwd = ComputeForward();
            Gizmos.DrawLine(transform.position, transform.position + fwd * lurchDistance);
        }
#endif
    }
}
