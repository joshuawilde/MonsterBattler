using System.Linq;
using MonsterBattler.Editor.MCP.Util;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MonsterBattler.Editor.MCP.Handlers
{
    [InitializeOnLoad]
    public static class SceneHandlers
    {
        static SceneHandlers()
        {
            MCPCommandRegistry.Register("scene.list_open", _ =>
            {
                var arr = new JArray();
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var s = SceneManager.GetSceneAt(i);
                    arr.Add(new JObject { ["name"] = s.name, ["path"] = s.path, ["loaded"] = s.isLoaded, ["isDirty"] = s.isDirty });
                }
                return arr;
            });

            MCPCommandRegistry.Register("scene.open", p =>
            {
                var path = (string)p["path"] ?? throw new System.ArgumentException("path required");
                var modeStr = (string)p["mode"] ?? "Single";
                var mode = modeStr == "Additive" ? OpenSceneMode.Additive : OpenSceneMode.Single;
                var scene = EditorSceneManager.OpenScene(path, mode);
                return new JObject { ["name"] = scene.name, ["path"] = scene.path };
            });

            MCPCommandRegistry.Register("scene.save_active", _ =>
            {
                var active = SceneManager.GetActiveScene();
                bool ok = EditorSceneManager.SaveScene(active);
                return new JObject { ["saved"] = ok, ["path"] = active.path };
            });

            MCPCommandRegistry.Register("scene.new", p =>
            {
                var setup = NewSceneSetup.EmptyScene;
                var modeStr = (string)p["mode"] ?? "Single";
                var mode = modeStr == "Additive" ? NewSceneMode.Additive : NewSceneMode.Single;
                var scene = EditorSceneManager.NewScene(setup, mode);
                var savePath = (string)p["path"];
                if (!string.IsNullOrEmpty(savePath))
                    EditorSceneManager.SaveScene(scene, savePath);
                return new JObject { ["name"] = scene.name, ["path"] = scene.path };
            });

            MCPCommandRegistry.Register("scene.get_hierarchy", p =>
            {
                var maxDepth = p["maxDepth"]?.Value<int>() ?? 64;
                var includeComponents = p["includeComponents"]?.Value<bool>() ?? true;
                var root = new JArray();
                var active = SceneManager.GetActiveScene();
                foreach (var go in active.GetRootGameObjects())
                    root.Add(DescribeNode(go.transform, 0, maxDepth, includeComponents));
                return new JObject { ["scene"] = active.name, ["roots"] = root };
            });

            MCPCommandRegistry.Register("scene.get_object", p =>
            {
                var go = GameObjectLookup.Resolve(p);
                return DescribeFull(go);
            });

            MCPCommandRegistry.Register("scene.find_by_component", p =>
            {
                var typeName = (string)p["type"] ?? throw new System.ArgumentException("type required");
                var type = TypeResolver.Resolve(typeName);
                var found = Object.FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None);
                var arr = new JArray();
                foreach (var obj in found)
                {
                    if (obj is Component c)
                        arr.Add(new JObject { ["path"] = GameObjectLookup.PathOf(c.gameObject), ["id"] = c.gameObject.GetInstanceID() });
                }
                return arr;
            });
        }

        static JObject DescribeNode(Transform t, int depth, int maxDepth, bool includeComponents)
        {
            var node = new JObject
            {
                ["name"] = t.name,
                ["path"] = GameObjectLookup.PathOf(t.gameObject),
                ["id"] = t.gameObject.GetInstanceID(),
                ["active"] = t.gameObject.activeSelf,
                ["childCount"] = t.childCount,
            };
            if (includeComponents)
            {
                var comps = new JArray();
                foreach (var c in t.GetComponents<Component>())
                {
                    if (c == null) { comps.Add("<missing>"); continue; }
                    comps.Add(c.GetType().Name);
                }
                node["components"] = comps;
            }
            if (depth < maxDepth && t.childCount > 0)
            {
                var children = new JArray();
                for (int i = 0; i < t.childCount; i++)
                    children.Add(DescribeNode(t.GetChild(i), depth + 1, maxDepth, includeComponents));
                node["children"] = children;
            }
            return node;
        }

        static JObject DescribeFull(GameObject go)
        {
            var obj = new JObject
            {
                ["name"] = go.name,
                ["path"] = GameObjectLookup.PathOf(go),
                ["id"] = go.GetInstanceID(),
                ["active"] = go.activeSelf,
                ["tag"] = go.tag,
                ["layer"] = go.layer,
                ["position"] = ToJson(go.transform.position),
                ["localPosition"] = ToJson(go.transform.localPosition),
                ["localEulerAngles"] = ToJson(go.transform.localEulerAngles),
                ["localScale"] = ToJson(go.transform.localScale),
            };
            var comps = new JArray();
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null) continue;
                comps.Add(new JObject { ["type"] = c.GetType().FullName, ["id"] = c.GetInstanceID() });
            }
            obj["components"] = comps;
            return obj;
        }

        static JArray ToJson(Vector3 v) => new JArray { v.x, v.y, v.z };
    }
}
