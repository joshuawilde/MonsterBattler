using System.Collections;
using UnityEngine;

namespace MonsterBattler.Game.Anim
{
    /// <summary>
    /// Universal monster death: flash to solid white, hold, fade to transparent, deactivate.
    /// Works on any rigged creature regardless of skeleton — we swap every renderer's materials
    /// to a single runtime white instance, then animate the white instance's alpha. The original
    /// materials are cached and restored if <see cref="Reset()"/> is called (e.g. on retry screens).
    ///
    /// Why this instead of a bone-slump: a generic "go limp" animation has to make pose decisions
    /// per body plan (quadrupeds buckle, bipeds topple), and the same animation playing on every
    /// faint gets stale fast. A flash reads as "defeated" instantly and looks identical across
    /// creatures — exactly the right vibe for an arcadey battle loop.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProceduralDeath : MonoBehaviour
    {
        [Header("Timing (seconds)")]
        [Range(0f, 0.5f)] public float rampDuration = 0.06f;
        [Range(0f, 1f)]   public float holdDuration = 0.18f;
        [Range(0f, 1f)]   public float fadeDuration = 0.18f;

        [Tooltip("Deactivate the GameObject after the fade completes. Off = leave it visible at 0 alpha.")]
        public bool deactivateAtEnd = true;

        bool _dying;
        Renderer[] _renderers;
        Material[][] _originalMats;
        Material _whiteMat;

        /// <summary>Fire-and-forget. Idempotent.</summary>
        public Coroutine Die() => _dying ? null : StartCoroutine(Run());

        [ContextMenu("Die (test)")] void DieFromMenu() => Die();

        IEnumerator Run()
        {
            _dying = true;

            var idle = GetComponent<ProceduralIdle>(); if (idle != null) idle.enabled = false;
            var atk  = GetComponent<ProceduralAttack>(); if (atk  != null) atk.enabled  = false;

            _renderers = GetComponentsInChildren<Renderer>(includeInactive: false);
            _originalMats = new Material[_renderers.Length][];
            _whiteMat = CreateWhiteMaterial();

            // Phase 1: ramp from original → white. We tint the white material's alpha 0→1 while
            // each renderer holds an *additional* per-mesh blend that starts at its own materials
            // and ends at the shared white. Implementation trick: swap to white immediately at
            // alpha 0 (invisible white), lerp alpha to 1 — meanwhile keep originals rendered too
            // would require two passes. Simpler: just instant-swap, but ramp via a brief alpha-up.
            for (int i = 0; i < _renderers.Length; i++)
            {
                _originalMats[i] = _renderers[i].sharedMaterials;
                var whiteArr = new Material[_originalMats[i].Length];
                for (int j = 0; j < whiteArr.Length; j++) whiteArr[j] = _whiteMat;
                _renderers[i].sharedMaterials = whiteArr;
            }

            // Ramp alpha 0→1 (the model fades INTO the white).
            yield return Tween(0f, 1f, rampDuration);

            // Hold solid white.
            float t = 0f;
            while (t < holdDuration) { t += Time.deltaTime; yield return null; }

            // Fade alpha 1→0 (white silhouette dissolves to nothing).
            yield return Tween(1f, 0f, fadeDuration);

            if (deactivateAtEnd) gameObject.SetActive(false);
        }

        IEnumerator Tween(float fromAlpha, float toAlpha, float duration)
        {
            if (duration <= 0f) { SetWhiteAlpha(toAlpha); yield break; }
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / duration);
                SetWhiteAlpha(Mathf.Lerp(fromAlpha, toAlpha, u));
                yield return null;
            }
            SetWhiteAlpha(toAlpha);
        }

        void SetWhiteAlpha(float a)
        {
            // Belt-and-suspenders: both URP and Built-in/legacy property names.
            var c = new Color(1f, 1f, 1f, a);
            if (_whiteMat.HasProperty("_BaseColor")) _whiteMat.SetColor("_BaseColor", c);
            if (_whiteMat.HasProperty("_Color"))     _whiteMat.SetColor("_Color", c);
        }

        /// <summary>Restore the original materials on every renderer. Use this for retry screens.</summary>
        public void Reset()
        {
            if (_renderers == null) return;
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null && _originalMats[i] != null)
                    _renderers[i].sharedMaterials = _originalMats[i];
            if (_whiteMat != null) Destroy(_whiteMat);
            _whiteMat = null;
            _dying = false;
            if (gameObject != null) gameObject.SetActive(true);
        }

        static Material CreateWhiteMaterial()
        {
            // Try URP first (this project uses URP), fall back to legacy.
            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Unlit/Transparent");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            var m = new Material(sh) { name = "ProceduralDeath_White" };

            // Configure as transparent so alpha actually fades.
            if (sh.name.Contains("Universal Render Pipeline"))
            {
                m.SetFloat("_Surface", 1f);     // 1 = Transparent
                m.SetFloat("_Blend", 0f);       // 0 = Alpha
                m.SetFloat("_ZWrite", 0f);
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.DisableKeyword("_ALPHATEST_ON");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            // Start invisible; the first Tween ramps to alpha=1.
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0f));
            if (m.HasProperty("_Color"))     m.SetColor("_Color",     new Color(1f, 1f, 1f, 0f));
            return m;
        }
    }
}
