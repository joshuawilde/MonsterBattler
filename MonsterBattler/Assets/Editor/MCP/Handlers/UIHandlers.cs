using System;
using MonsterBattler.Editor.MCP.Util;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MonsterBattler.Editor.MCP.Handlers
{
    /// <summary>
    /// uGUI sugar. Most UI ops are doable via component.add + component.set_field, but
    /// RectTransform is fiddly enough that ui.set_rect is worth a dedicated handler.
    /// </summary>
    [InitializeOnLoad]
    public static class UIHandlers
    {
        static UIHandlers()
        {
            MCPCommandRegistry.Register("ui.create_canvas", p =>
            {
                var name = (string)p["name"] ?? "Canvas";
                var renderModeStr = (string)p["renderMode"] ?? "ScreenSpaceOverlay";
                if (!Enum.TryParse<RenderMode>(renderModeStr, out var renderMode))
                    throw new ArgumentException($"Unknown renderMode '{renderModeStr}'");

                var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                var canvas = go.GetComponent<Canvas>();
                canvas.renderMode = renderMode;
                var scaler = go.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920); // portrait mobile default
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                // Ensure an EventSystem exists in the scene.
                if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
                {
                    var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                    Undo.RegisterCreatedObjectUndo(es, "MCP Create EventSystem");
                }

                Undo.RegisterCreatedObjectUndo(go, "MCP Create Canvas");
                EditorSceneManager.MarkSceneDirty(go.scene);
                return new JObject { ["path"] = GameObjectLookup.PathOf(go), ["id"] = go.GetInstanceID() };
            });

            MCPCommandRegistry.Register("ui.set_rect", p =>
            {
                var go = GameObjectLookup.Resolve(p);
                var rt = go.GetComponent<RectTransform>();
                if (rt == null) throw new InvalidOperationException($"{go.name} has no RectTransform");
                Undo.RecordObject(rt, "MCP Set Rect");
                if (p["anchorMin"] is JArray amin) rt.anchorMin = ToVec2(amin);
                if (p["anchorMax"] is JArray amax) rt.anchorMax = ToVec2(amax);
                if (p["pivot"] is JArray piv) rt.pivot = ToVec2(piv);
                if (p["anchoredPosition"] is JArray ap) rt.anchoredPosition = ToVec2(ap);
                if (p["sizeDelta"] is JArray sd) rt.sizeDelta = ToVec2(sd);
                if (p["offsetMin"] is JArray omin) rt.offsetMin = ToVec2(omin);
                if (p["offsetMax"] is JArray omax) rt.offsetMax = ToVec2(omax);
                EditorSceneManager.MarkSceneDirty(go.scene);
                return new JObject { ["ok"] = true };
            });

            // Create a TextMeshPro UI label (project standard — all UI text is TMP). The default
            // TMP font asset is auto-assigned, so it renders without extra setup.
            MCPCommandRegistry.Register("ui.create_text", p =>
            {
                var go = CreateUIChild(p);
                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text = (string)p["text"] ?? "";
                tmp.fontSize = (float?)p["fontSize"] ?? 24f;
                tmp.color = p["color"] is JArray c ? ToColor(c) : Color.white;
                tmp.alignment = ParseTmpAlign((string)p["alignment"], TextAlignmentOptions.TopLeft);
                tmp.enableWordWrapping = true;
                if ((bool?)p["bestFit"] == true)
                {
                    tmp.enableAutoSizing = true;
                    tmp.fontSizeMin = 6f;
                    tmp.fontSizeMax = (float?)p["fontSize"] ?? 24f;
                }
                Undo.RegisterCreatedObjectUndo(go, "MCP Create Text");
                EditorSceneManager.MarkSceneDirty(go.scene);
                return new JObject { ["path"] = GameObjectLookup.PathOf(go), ["id"] = go.GetInstanceID() };
            });

            // Replace a GameObject's legacy UnityEngine.UI.Text with a TextMeshProUGUI, preserving
            // text / size / color / alignment / wrap / best-fit. References to the old component
            // break (it's a new component), so re-wire serialized fields after converting.
            MCPCommandRegistry.Register("ui.text_to_tmp", p =>
            {
                var go = GameObjectLookup.Resolve(p);
                var old = go.GetComponent<Text>();
                if (old == null) return new JObject { ["converted"] = false };
                string text = old.text;
                float size = old.fontSize;
                Color color = old.color;
                bool wrap = old.horizontalOverflow == HorizontalWrapMode.Wrap;
                bool bestFit = old.resizeTextForBestFit;
                int minS = old.resizeTextMinSize, maxS = old.resizeTextMaxSize;
                bool raycast = old.raycastTarget;
                var align = TextAnchorToTmp(old.alignment);
                Undo.DestroyObjectImmediate(old);
                var tmp = Undo.AddComponent<TextMeshProUGUI>(go);
                tmp.text = text;
                tmp.color = color;
                tmp.alignment = align;
                tmp.enableWordWrapping = wrap;
                tmp.raycastTarget = raycast;
                if (bestFit) { tmp.enableAutoSizing = true; tmp.fontSizeMin = minS; tmp.fontSizeMax = maxS; tmp.fontSize = maxS; }
                else { tmp.fontSize = size; }
                EditorSceneManager.MarkSceneDirty(go.scene);
                return new JObject { ["converted"] = true, ["id"] = tmp.GetInstanceID() };
            });

            // Create a uGUI Image (gets RectTransform + CanvasRenderer automatically).
            MCPCommandRegistry.Register("ui.create_image", p =>
            {
                var go = CreateUIChild(p);
                var img = go.AddComponent<Image>();
                img.color = p["color"] is JArray c ? ToColor(c) : Color.white;
                if ((bool?)p["raycastTarget"] == false) img.raycastTarget = false;
                Undo.RegisterCreatedObjectUndo(go, "MCP Create Image");
                EditorSceneManager.MarkSceneDirty(go.scene);
                return new JObject { ["path"] = GameObjectLookup.PathOf(go), ["id"] = go.GetInstanceID() };
            });
        }

        static GameObject CreateUIChild(JObject p)
        {
            var go = new GameObject((string)p["name"] ?? "UIElement", typeof(RectTransform));
            if (p["parent"] is JObject par)
            {
                var parent = GameObjectLookup.Resolve(par);
                if (parent != null) go.transform.SetParent(parent.transform, worldPositionStays: false);
            }
            return go;
        }

        static Vector2 ToVec2(JArray a) => new Vector2((float)a[0], (float)a[1]);
        static Color ToColor(JArray a) =>
            new Color((float)a[0], (float)a[1], (float)a[2], a.Count > 3 ? (float)a[3] : 1f);

        // Accept either a TextAnchor name (e.g. "MiddleCenter") or a TMP name (e.g. "Center").
        static TextAlignmentOptions ParseTmpAlign(string s, TextAlignmentOptions fallback)
        {
            if (string.IsNullOrEmpty(s)) return fallback;
            if (System.Enum.TryParse<TextAlignmentOptions>(s, out var direct)) return direct;
            return System.Enum.TryParse<TextAnchor>(s, out var anchor) ? TextAnchorToTmp(anchor) : fallback;
        }

        static TextAlignmentOptions TextAnchorToTmp(TextAnchor a) => a switch
        {
            TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
            TextAnchor.UpperCenter => TextAlignmentOptions.Top,
            TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
            TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
            TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
            TextAnchor.MiddleRight => TextAlignmentOptions.Right,
            TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
            TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
            TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
            _ => TextAlignmentOptions.TopLeft,
        };
    }
}
