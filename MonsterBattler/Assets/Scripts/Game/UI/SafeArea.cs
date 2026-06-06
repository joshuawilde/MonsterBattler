using UnityEngine;

namespace MonsterBattler.Game.UI
{
    /// <summary>
    /// Insets this RectTransform to the device safe area — the region not covered by notches, the
    /// Dynamic Island, rounded corners, or the home indicator (<see cref="Screen.safeArea"/>).
    ///
    /// Put the battle UI under a GameObject carrying this component so nothing hides behind hardware.
    /// The owning RectTransform must be a full-screen-stretch child of the Canvas (anchors 0,0–1,1).
    ///
    /// Runs in edit mode (<see cref="ExecuteAlways"/>) so the inset is visible while authoring and
    /// tracks Unity's Device Simulator, and re-applies at runtime on rotation / resolution change.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public sealed class SafeArea : MonoBehaviour
    {
        [Tooltip("Apply the left/right insets (notches in landscape, rounded corners).")]
        [SerializeField] bool _applyLeftRight = true;
        [Tooltip("Apply the top/bottom insets (status bar / notch, home indicator).")]
        [SerializeField] bool _applyTopBottom = true;

        RectTransform _rt;
        Rect _lastSafe;
        Vector2Int _lastScreen;

        void OnEnable()
        {
            _rt = GetComponent<RectTransform>();
            Apply();
        }

        void Update()
        {
            // Safe area, resolution and orientation can all change at runtime — and in the Device
            // Simulator while editing. The comparison is cheap; we only rewrite anchors on change.
            if (Screen.safeArea != _lastSafe ||
                Screen.width != _lastScreen.x || Screen.height != _lastScreen.y)
                Apply();
        }

        /// <summary>Recompute and apply the safe-area anchors. Safe to call any time.</summary>
        public void Apply()
        {
            if (_rt == null) _rt = GetComponent<RectTransform>();
            int w = Screen.width, h = Screen.height;
            if (w <= 0 || h <= 0) return;

            var safe = Screen.safeArea;
            // Outside the Device Simulator, the editor's Screen.* can reflect the focused editor
            // window rather than the Game view, yielding a safe area that doesn't fit the screen.
            // Ignore such readings so we never bake bad anchors; the Simulator and real devices
            // always report a safe area within [0,0]–[w,h].
            if (safe.width <= 0f || safe.height <= 0f ||
                safe.xMin < -0.5f || safe.yMin < -0.5f ||
                safe.xMax > w + 0.5f || safe.yMax > h + 0.5f)
                return;

            _lastSafe = safe;
            _lastScreen = new Vector2Int(w, h);

            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= w; min.y /= h;
            max.x /= w; max.y /= h;

            if (!_applyLeftRight) { min.x = 0f; max.x = 1f; }
            if (!_applyTopBottom) { min.y = 0f; max.y = 1f; }

            // Clamp to the canvas as a final guard, and ignore degenerate results.
            min.x = Mathf.Clamp01(min.x); min.y = Mathf.Clamp01(min.y);
            max.x = Mathf.Clamp01(max.x); max.y = Mathf.Clamp01(max.y);
            if (max.x <= min.x || max.y <= min.y) return;

            _rt.anchorMin = min;
            _rt.anchorMax = max;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }
    }
}
