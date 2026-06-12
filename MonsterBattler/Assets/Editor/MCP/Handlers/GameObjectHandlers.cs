using MonsterBattler.Editor.MCP.Util;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MonsterBattler.Editor.MCP.Handlers
{
    [InitializeOnLoad]
    public static class GameObjectHandlers
    {
        static GameObjectHandlers()
        {
            MCPCommandRegistry.Register("gameobject.create", p =>
            {
                var name = (string)p["name"] ?? "GameObject";
                var primitive = (string)p["primitive"]; // optional: "Cube"/"Sphere"/...
                GameObject go;
                if (!string.IsNullOrEmpty(primitive))
                {
                    if (!System.Enum.TryParse<PrimitiveType>(primitive, out var pt))
                        throw new System.ArgumentException($"Unknown primitive '{primitive}'");
                    go = GameObject.CreatePrimitive(pt);
                    go.name = name;
                }
                else
                {
                    go = new GameObject(name);
                }
                var parent = GameObjectLookup.Resolve(p["parent"] as JObject, required: false);
                if (parent != null) Undo.SetTransformParent(go.transform, parent.transform, "MCP Create");
                Undo.RegisterCreatedObjectUndo(go, "MCP Create");
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                return new JObject { ["path"] = GameObjectLookup.PathOf(go), ["id"] = go.GetInstanceID() };
            });

            MCPCommandRegistry.Register("gameobject.delete", p =>
            {
                var go = GameObjectLookup.Resolve(p);
                Undo.DestroyObjectImmediate(go);
                return new JObject { ["deleted"] = true };
            });

            MCPCommandRegistry.Register("gameobject.rename", p =>
            {
                var go = GameObjectLookup.Resolve(p);
                var newName = (string)p["newName"] ?? throw new System.ArgumentException("newName required");
                Undo.RecordObject(go, "MCP Rename");
                go.name = newName;
                EditorSceneManager.MarkSceneDirty(go.scene);
                return new JObject { ["path"] = GameObjectLookup.PathOf(go), ["id"] = go.GetInstanceID() };
            });

            MCPCommandRegistry.Register("gameobject.set_active", p =>
            {
                var go = GameObjectLookup.Resolve(p);
                var active = p["active"]?.Value<bool>() ?? throw new System.ArgumentException("active required");
                Undo.RecordObject(go, "MCP SetActive");
                go.SetActive(active);
                EditorSceneManager.MarkSceneDirty(go.scene);
                return new JObject { ["active"] = go.activeSelf };
            });

            MCPCommandRegistry.Register("gameobject.reparent", p =>
            {
                var go = GameObjectLookup.Resolve(p);
                var newParent = GameObjectLookup.Resolve(p["parent"] as JObject, required: false);
                var worldPositionStays = p["worldPositionStays"]?.Value<bool>() ?? true;
                Undo.SetTransformParent(go.transform, newParent?.transform, worldPositionStays, "MCP Reparent");
                EditorSceneManager.MarkSceneDirty(go.scene);
                return new JObject { ["path"] = GameObjectLookup.PathOf(go) };
            });

            // Reorder among siblings (z-order for UI): 0 = behind, -1 = front (last sibling).
            MCPCommandRegistry.Register("gameobject.set_sibling_index", p =>
            {
                var go = GameObjectLookup.Resolve(p);
                int index = p["index"]?.Value<int>() ?? throw new System.ArgumentException("index required");
                Undo.RecordObject(go.transform, "MCP SetSiblingIndex");
                if (index < 0) go.transform.SetAsLastSibling();
                else go.transform.SetSiblingIndex(index);
                EditorSceneManager.MarkSceneDirty(go.scene);
                return new JObject { ["index"] = go.transform.GetSiblingIndex() };
            });

            MCPCommandRegistry.Register("gameobject.set_transform", p =>
            {
                var go = GameObjectLookup.Resolve(p);
                Undo.RecordObject(go.transform, "MCP SetTransform");
                if (p["localPosition"] is JArray lp) go.transform.localPosition = ToVec3(lp);
                if (p["position"] is JArray wp) go.transform.position = ToVec3(wp);
                if (p["localEulerAngles"] is JArray le) go.transform.localEulerAngles = ToVec3(le);
                if (p["eulerAngles"] is JArray we) go.transform.eulerAngles = ToVec3(we);
                if (p["localScale"] is JArray ls) go.transform.localScale = ToVec3(ls);
                EditorSceneManager.MarkSceneDirty(go.scene);
                return new JObject { ["ok"] = true };
            });
        }

        static Vector3 ToVec3(JArray a) => new Vector3((float)a[0], (float)a[1], (float)a[2]);
    }
}
