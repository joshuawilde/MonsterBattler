using System;
using MonsterBattler.Editor.MCP.Util;
using Newtonsoft.Json.Linq;
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
        }

        static Vector2 ToVec2(JArray a) => new Vector2((float)a[0], (float)a[1]);
    }
}
