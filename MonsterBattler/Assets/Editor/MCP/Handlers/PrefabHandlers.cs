using System;
using MonsterBattler.Editor.MCP.Util;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MonsterBattler.Editor.MCP.Handlers
{
    [InitializeOnLoad]
    public static class PrefabHandlers
    {
        static PrefabHandlers()
        {
            MCPCommandRegistry.Register("prefab.instantiate", p =>
            {
                var assetPath = (string)p["assetPath"] ?? throw new ArgumentException("assetPath required");
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab == null) throw new InvalidOperationException($"No prefab at '{assetPath}'");
                var parent = GameObjectLookup.Resolve(p["parent"] as JObject, required: false);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent != null ? parent.transform : null);
                if (instance == null) throw new InvalidOperationException("Instantiation failed");
                var name = (string)p["name"];
                if (!string.IsNullOrEmpty(name)) instance.name = name;
                Undo.RegisterCreatedObjectUndo(instance, "MCP Instantiate Prefab");
                EditorSceneManager.MarkSceneDirty(instance.scene);
                return new JObject { ["path"] = GameObjectLookup.PathOf(instance), ["id"] = instance.GetInstanceID() };
            });

            MCPCommandRegistry.Register("prefab.save_as", p =>
            {
                var go = GameObjectLookup.Resolve(p);
                var assetPath = (string)p["assetPath"] ?? throw new ArgumentException("assetPath required");
                var connect = p["connectInstance"]?.Value<bool>() ?? true;
                GameObject saved;
                if (connect)
                    saved = PrefabUtility.SaveAsPrefabAssetAndConnect(go, assetPath, InteractionMode.UserAction);
                else
                    saved = PrefabUtility.SaveAsPrefabAsset(go, assetPath);
                if (saved == null) throw new InvalidOperationException("Failed to save prefab");
                return new JObject { ["assetPath"] = assetPath };
            });

            MCPCommandRegistry.Register("prefab.apply_overrides", p =>
            {
                var go = GameObjectLookup.Resolve(p);
                if (!PrefabUtility.IsPartOfPrefabInstance(go))
                    throw new InvalidOperationException("Not a prefab instance");
                PrefabUtility.ApplyPrefabInstance(go, InteractionMode.UserAction);
                return new JObject { ["applied"] = true };
            });

            MCPCommandRegistry.Register("prefab.list", p =>
            {
                var folder = (string)p["folder"] ?? "Assets";
                var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
                var arr = new JArray();
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    arr.Add(new JObject { ["assetPath"] = path, ["name"] = System.IO.Path.GetFileNameWithoutExtension(path) });
                }
                return arr;
            });
        }
    }
}
