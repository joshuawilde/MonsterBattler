using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MonsterBattler.Sim.Effects
{
    /// <summary>
    /// Singleton registry mapping <c>EffectId</c> → effect instance. Effects auto-register
    /// themselves on first access via reflection: every non-abstract <see cref="Effect"/>
    /// subclass in the loaded assemblies gets instantiated once and indexed by its EffectId.
    /// </summary>
    public static class EffectRegistry
    {
        static Dictionary<string, Effect> s_byId;
        static readonly object s_lock = new();

        public static Effect Get(string effectId)
        {
            if (string.IsNullOrEmpty(effectId)) return null;
            EnsureLoaded();
            return s_byId.TryGetValue(effectId, out var e) ? e : null;
        }

        public static IReadOnlyDictionary<string, Effect> All
        {
            get { EnsureLoaded(); return s_byId; }
        }

        static void EnsureLoaded()
        {
            if (s_byId != null) return;
            lock (s_lock)
            {
                if (s_byId != null) return;
                var dict = new Dictionary<string, Effect>(StringComparer.Ordinal);
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type[] types;
                    try { types = asm.GetTypes(); }
                    catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray(); }
                    foreach (var t in types)
                    {
                        if (t == null || t.IsAbstract) continue;
                        if (!typeof(Effect).IsAssignableFrom(t)) continue;
                        // Need a public parameterless constructor.
                        if (t.GetConstructor(Type.EmptyTypes) == null) continue;
                        var instance = (Effect)Activator.CreateInstance(t);
                        if (string.IsNullOrEmpty(instance.EffectId)) continue;
                        if (dict.ContainsKey(instance.EffectId))
                            throw new InvalidOperationException(
                                $"Duplicate EffectId '{instance.EffectId}' between {dict[instance.EffectId].GetType().FullName} and {t.FullName}");
                        dict[instance.EffectId] = instance;
                    }
                }
                s_byId = dict;
            }
        }
    }
}
