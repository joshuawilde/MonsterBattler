using System;
using System.Reflection;
using MonsterBattler.Editor.MCP.Util;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MonsterBattler.Editor.MCP.Handlers
{
    [InitializeOnLoad]
    public static class ComponentHandlers
    {
        static ComponentHandlers()
        {
            MCPCommandRegistry.Register("component.add", p =>
            {
                var go = GameObjectLookup.Resolve(p);
                var typeName = (string)p["type"] ?? throw new ArgumentException("type required");
                var type = TypeResolver.Resolve(typeName);
                if (!typeof(Component).IsAssignableFrom(type))
                    throw new InvalidOperationException($"{type.FullName} is not a Component");
                var c = Undo.AddComponent(go, type);
                EditorSceneManager.MarkSceneDirty(go.scene);
                return new JObject { ["id"] = c.GetInstanceID(), ["type"] = c.GetType().FullName };
            });

            MCPCommandRegistry.Register("component.remove", p =>
            {
                var go = GameObjectLookup.Resolve(p);
                var typeName = (string)p["type"] ?? throw new ArgumentException("type required");
                var type = TypeResolver.Resolve(typeName);
                var c = go.GetComponent(type);
                if (c == null) throw new InvalidOperationException($"No {type.Name} on {go.name}");
                Undo.DestroyObjectImmediate(c);
                EditorSceneManager.MarkSceneDirty(go.scene);
                return new JObject { ["removed"] = true };
            });

            MCPCommandRegistry.Register("component.get", p =>
            {
                var c = ResolveComponent(p);
                var so = new SerializedObject(c);
                var fields = new JObject();
                var it = so.GetIterator();
                bool enter = true;
                while (it.NextVisible(enter))
                {
                    enter = false;
                    fields[it.propertyPath] = SerializedValueToJson(it);
                }
                return new JObject { ["id"] = c.GetInstanceID(), ["type"] = c.GetType().FullName, ["fields"] = fields };
            });

            MCPCommandRegistry.Register("component.set_field", p =>
            {
                var c = ResolveComponent(p);
                var path = (string)p["field"] ?? throw new ArgumentException("field required");
                var value = p["value"];
                var so = new SerializedObject(c);
                var prop = so.FindProperty(path);
                if (prop == null)
                    throw new InvalidOperationException($"No serialized property '{path}' on {c.GetType().FullName}");
                ApplyJsonToProperty(prop, value);
                so.ApplyModifiedProperties();
                if (c is Component comp) EditorSceneManager.MarkSceneDirty(comp.gameObject.scene);
                return new JObject { ["ok"] = true, ["field"] = path };
            });

            MCPCommandRegistry.Register("component.set_fields", p =>
            {
                var c = ResolveComponent(p);
                var fields = p["fields"] as JObject ?? throw new ArgumentException("fields required");
                var so = new SerializedObject(c);
                foreach (var kv in fields)
                {
                    var prop = so.FindProperty(kv.Key);
                    if (prop == null)
                        throw new InvalidOperationException($"No serialized property '{kv.Key}' on {c.GetType().FullName}");
                    ApplyJsonToProperty(prop, kv.Value);
                }
                so.ApplyModifiedProperties();
                if (c is Component comp) EditorSceneManager.MarkSceneDirty(comp.gameObject.scene);
                return new JObject { ["ok"] = true, ["count"] = fields.Count };
            });
        }

        static Component ResolveComponent(JObject p)
        {
            // Either: { id: componentInstanceId } or { path/id: <go>, type: "X" }
            var idTok = p["componentId"];
            if (idTok != null && idTok.Type != JTokenType.Null)
            {
                var obj = EditorUtility.InstanceIDToObject(idTok.Value<int>()) as Component;
                if (obj == null) throw new InvalidOperationException("componentId did not resolve to a Component");
                return obj;
            }
            var go = GameObjectLookup.Resolve(p);
            var typeName = (string)p["type"] ?? throw new ArgumentException("type required (or componentId)");
            var type = TypeResolver.Resolve(typeName);
            var c = go.GetComponent(type);
            if (c == null) throw new InvalidOperationException($"No {type.Name} on {go.name}");
            return c;
        }

        static void ApplyJsonToProperty(SerializedProperty prop, JToken value)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:        prop.intValue = value.Value<int>(); break;
                case SerializedPropertyType.Boolean:        prop.boolValue = value.Value<bool>(); break;
                case SerializedPropertyType.Float:          prop.floatValue = value.Value<float>(); break;
                case SerializedPropertyType.String:         prop.stringValue = value.Value<string>() ?? ""; break;
                case SerializedPropertyType.Color:          prop.colorValue = ToColor(value); break;
                case SerializedPropertyType.Enum:           prop.enumValueIndex = ResolveEnumIndex(prop, value); break;
                case SerializedPropertyType.Vector2:        prop.vector2Value = ToVec2(value); break;
                case SerializedPropertyType.Vector3:        prop.vector3Value = ToVec3(value); break;
                case SerializedPropertyType.Vector4:        prop.vector4Value = ToVec4(value); break;
                case SerializedPropertyType.Quaternion:     prop.quaternionValue = ToQuat(value); break;
                case SerializedPropertyType.Rect:           prop.rectValue = ToRect(value); break;
                case SerializedPropertyType.Bounds:         prop.boundsValue = ToBounds(value); break;
                case SerializedPropertyType.ObjectReference:prop.objectReferenceValue = ResolveObjectReference(value, prop); break;
                case SerializedPropertyType.LayerMask:      prop.intValue = value.Value<int>(); break;
                case SerializedPropertyType.ArraySize:      prop.arraySize = value.Value<int>(); break;
                default:
                    throw new InvalidOperationException($"Unsupported serialized property type {prop.propertyType} at {prop.propertyPath}");
            }
        }

        static JToken SerializedValueToJson(SerializedProperty p)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.Integer:   return p.intValue;
                case SerializedPropertyType.Boolean:   return p.boolValue;
                case SerializedPropertyType.Float:     return p.floatValue;
                case SerializedPropertyType.String:    return p.stringValue;
                case SerializedPropertyType.Color:     var c = p.colorValue; return new JArray { c.r, c.g, c.b, c.a };
                case SerializedPropertyType.Enum:      return p.enumValueIndex >= 0 && p.enumValueIndex < p.enumNames.Length ? p.enumNames[p.enumValueIndex] : null;
                case SerializedPropertyType.Vector2:   var v2 = p.vector2Value; return new JArray { v2.x, v2.y };
                case SerializedPropertyType.Vector3:   var v3 = p.vector3Value; return new JArray { v3.x, v3.y, v3.z };
                case SerializedPropertyType.Vector4:   var v4 = p.vector4Value; return new JArray { v4.x, v4.y, v4.z, v4.w };
                case SerializedPropertyType.Quaternion:var q = p.quaternionValue; return new JArray { q.x, q.y, q.z, q.w };
                case SerializedPropertyType.ObjectReference:
                    var o = p.objectReferenceValue;
                    return o == null ? null : (JToken)new JObject { ["id"] = o.GetInstanceID(), ["type"] = o.GetType().FullName, ["name"] = o.name };
                case SerializedPropertyType.LayerMask: return p.intValue;
                default: return $"<{p.propertyType}>";
            }
        }

        static UnityEngine.Object ResolveObjectReference(JToken value, SerializedProperty contextProp)
        {
            if (value == null || value.Type == JTokenType.Null) return null;
            if (value is JObject o)
            {
                if (o["id"] != null) return EditorUtility.InstanceIDToObject(o["id"].Value<int>());
                if (o["assetPath"] != null)
                {
                    var path = (string)o["assetPath"];
                    var typeName = (string)o["assetType"];
                    if (!string.IsNullOrEmpty(typeName))
                    {
                        var t = TypeResolver.Resolve(typeName);
                        return AssetDatabase.LoadAssetAtPath(path, t);
                    }
                    return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                }
                if (o["sceneObjectPath"] != null)
                {
                    var go = GameObjectLookup.FindByPath((string)o["sceneObjectPath"]);
                    if (go == null) return null;
                    var ctName = (string)o["componentType"];
                    if (string.IsNullOrEmpty(ctName)) return go;
                    var ct = TypeResolver.Resolve(ctName);
                    var comp = go.GetComponent(ct);
                    if (comp == null)
                        throw new InvalidOperationException($"GameObject '{(string)o["sceneObjectPath"]}' has no {ct.Name} component");
                    return comp;
                }
            }
            if (value.Type == JTokenType.Integer)
                return EditorUtility.InstanceIDToObject(value.Value<int>());
            throw new InvalidOperationException("Object reference must be { id } or { assetPath, assetType? } or { sceneObjectPath }");
        }

        static int ResolveEnumIndex(SerializedProperty prop, JToken value)
        {
            if (value.Type == JTokenType.Integer) return value.Value<int>();
            var s = value.Value<string>();
            for (int i = 0; i < prop.enumNames.Length; i++)
                if (prop.enumNames[i] == s) return i;
            throw new InvalidOperationException($"Enum value '{s}' not in {string.Join(",", prop.enumNames)}");
        }

        static Vector2 ToVec2(JToken t) { var a = (JArray)t; return new Vector2((float)a[0], (float)a[1]); }
        static Vector3 ToVec3(JToken t) { var a = (JArray)t; return new Vector3((float)a[0], (float)a[1], (float)a[2]); }
        static Vector4 ToVec4(JToken t) { var a = (JArray)t; return new Vector4((float)a[0], (float)a[1], (float)a[2], (float)a[3]); }
        static Quaternion ToQuat(JToken t) { var a = (JArray)t; return new Quaternion((float)a[0], (float)a[1], (float)a[2], (float)a[3]); }
        static Color ToColor(JToken t) { var a = (JArray)t; return new Color((float)a[0], (float)a[1], (float)a[2], a.Count > 3 ? (float)a[3] : 1f); }
        static Rect ToRect(JToken t) { var a = (JArray)t; return new Rect((float)a[0], (float)a[1], (float)a[2], (float)a[3]); }
        static Bounds ToBounds(JToken t) { var o = (JObject)t; return new Bounds(ToVec3(o["center"]), ToVec3(o["size"])); }
    }
}
