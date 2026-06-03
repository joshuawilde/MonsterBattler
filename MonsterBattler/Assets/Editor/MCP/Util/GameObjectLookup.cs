using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MonsterBattler.Editor.MCP.Util
{
    /// <summary>
    /// Resolves GameObjects from MCP params. Callers may pass either `path` (e.g. "Stage/Slot1/Sphere")
    /// or `id` (Unity instanceID, stable per session). Path wins if both are present.
    ///
    /// Paths are exhaustively searched: if more than one GameObject matches at any segment,
    /// resolution throws an AmbiguousPathException listing every candidate so the caller can
    /// disambiguate (rename, or switch to id-based addressing).
    /// </summary>
    public static class GameObjectLookup
    {
        public sealed class AmbiguousPathException : System.Exception
        {
            public AmbiguousPathException(string message) : base(message) { }
        }

        public static GameObject Resolve(JObject p, bool required = true)
        {
            var path = (string)p?["path"];
            var idTok = p?["id"];
            if (!string.IsNullOrEmpty(path))
            {
                var go = FindByPath(path);
                if (go == null && required) throw new System.InvalidOperationException($"No GameObject at path '{path}'");
                return go;
            }
            if (idTok != null && idTok.Type != JTokenType.Null)
            {
                var id = idTok.Value<int>();
                var go = EditorUtility.InstanceIDToObject(id) as GameObject;
                if (go == null && required) throw new System.InvalidOperationException($"No GameObject with instanceID {id}");
                return go;
            }
            if (required) throw new System.ArgumentException("Provide 'path' or 'id'");
            return null;
        }

        /// <summary>
        /// Exhaustive path search across every loaded scene. Throws <see cref="AmbiguousPathException"/>
        /// when more than one GameObject matches. Returns null when zero match.
        /// </summary>
        public static GameObject FindByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var segments = path.Split('/');

            // Collect all candidate roots first (multiple scenes/objects may share a root name).
            var frontier = new List<Transform>();
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                var scene = SceneManager.GetSceneAt(s);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                    if (root.name == segments[0]) frontier.Add(root.transform);
            }

            // Walk each remaining segment, expanding every matching child.
            for (int i = 1; i < segments.Length; i++)
            {
                var next = new List<Transform>();
                foreach (var node in frontier)
                {
                    for (int c = 0; c < node.childCount; c++)
                    {
                        var child = node.GetChild(c);
                        if (child.name == segments[i]) next.Add(child);
                    }
                }
                frontier = next;
                if (frontier.Count == 0) return null;
            }

            if (frontier.Count == 1) return frontier[0].gameObject;

            // Ambiguous — surface every candidate's full hierarchy path + scene + instance id.
            var sample = frontier
                .Take(8)
                .Select(t => $"  - {PathOf(t.gameObject)} (scene='{t.gameObject.scene.name}', id={t.gameObject.GetInstanceID()})")
                .ToList();
            var extra = frontier.Count > sample.Count ? $"\n  ... and {frontier.Count - sample.Count} more" : "";
            throw new AmbiguousPathException(
                $"Path '{path}' is ambiguous — {frontier.Count} matches:\n{string.Join("\n", sample)}{extra}\n" +
                "Resolve by renaming one of them, deepening the path, or addressing by 'id' instead.");
        }

        public static string PathOf(GameObject go)
        {
            if (go == null) return null;
            var stack = new Stack<string>();
            var t = go.transform;
            while (t != null) { stack.Push(t.name); t = t.parent; }
            return string.Join("/", stack);
        }
    }
}
