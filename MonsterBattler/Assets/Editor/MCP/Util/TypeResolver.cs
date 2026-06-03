using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MonsterBattler.Editor.MCP.Util
{
    /// <summary>
    /// Resolves a type by short or fully-qualified name across all loaded assemblies.
    /// Accepts: "UnityEngine.Transform", "Transform", "Image", "UnityEngine.UI.Image".
    /// </summary>
    public static class TypeResolver
    {
        static readonly Dictionary<string, Type> s_cache = new(StringComparer.Ordinal);

        public static Type Resolve(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("type name required");
            if (s_cache.TryGetValue(name, out var hit)) return hit;

            // Try direct type lookup (works for fully-qualified names).
            var t = Type.GetType(name, false);
            if (t == null)
            {
                // Search all loaded assemblies for a matching FullName or short Name.
                var candidates = new List<Type>();
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type[] types;
                    try { types = asm.GetTypes(); }
                    catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(x => x != null).ToArray(); }
                    foreach (var ty in types)
                    {
                        if (ty == null) continue;
                        if (ty.FullName == name || ty.Name == name)
                            candidates.Add(ty);
                    }
                }
                if (candidates.Count == 0) throw new InvalidOperationException($"Type '{name}' not found");
                if (candidates.Count > 1)
                {
                    // Prefer UnityEngine.* over user code on short-name collisions.
                    var preferred = candidates.FirstOrDefault(c => c.FullName.StartsWith("UnityEngine.")) ?? candidates[0];
                    t = preferred;
                }
                else t = candidates[0];
            }
            s_cache[name] = t;
            return t;
        }
    }
}
