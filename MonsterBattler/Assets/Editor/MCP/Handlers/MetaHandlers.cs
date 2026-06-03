using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MonsterBattler.Editor.MCP.Handlers
{
    [InitializeOnLoad]
    public static class MetaHandlers
    {
        static MetaHandlers()
        {
            MCPCommandRegistry.Register("meta.ping", _ => new JObject { ["pong"] = true, ["time"] = System.DateTime.UtcNow.ToString("o") });
            MCPCommandRegistry.Register("meta.version", _ => new JObject
            {
                ["unity"] = Application.unityVersion,
                ["bridge"] = "0.1.0",
                ["project"] = Application.productName,
            });
            MCPCommandRegistry.Register("meta.list_commands", _ =>
            {
                var arr = new JArray();
                foreach (var name in MCPCommandRegistry.RegisteredCommands.OrderBy(s => s)) arr.Add(name);
                return arr;
            });
            MCPCommandRegistry.Register("meta.refresh_assets", p =>
            {
                var save = p?["save"]?.Value<bool>() ?? false;
                if (save) AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return new JObject { ["refreshed"] = true };
            });
            MCPCommandRegistry.Register("meta.batch", p =>
            {
                var ops = p?["ops"] as JArray ?? new JArray();
                var stopOnError = p?["stopOnError"]?.Value<bool>() ?? true;
                var results = new JArray();
                foreach (var op in ops)
                {
                    var cmd = (string)op["command"];
                    var sub = op["params"] as JObject ?? new JObject();
                    JObject entry;
                    try
                    {
                        var r = MCPCommandRegistry.Dispatch(cmd, sub);
                        entry = new JObject { ["ok"] = true, ["result"] = r };
                    }
                    catch (System.Exception e)
                    {
                        entry = new JObject { ["ok"] = false, ["error"] = e.Message };
                        results.Add(entry);
                        if (stopOnError) return new JObject { ["results"] = results, ["stopped"] = true };
                        continue;
                    }
                    results.Add(entry);
                }
                return new JObject { ["results"] = results };
            });
        }
    }
}
