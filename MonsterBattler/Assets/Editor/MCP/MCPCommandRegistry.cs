using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace MonsterBattler.Editor.MCP
{
    /// <summary>
    /// Main-thread command registry. Handlers run on the Unity main thread, so they can
    /// freely use editor APIs. Return any JSON-serializable JToken (object, array, value).
    /// </summary>
    public static class MCPCommandRegistry
    {
        public delegate JToken Handler(JObject p);

        static readonly Dictionary<string, Handler> s_handlers = new(StringComparer.Ordinal);

        public static void Register(string name, Handler handler)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("name");
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            s_handlers[name] = handler;
        }

        public static IReadOnlyCollection<string> RegisteredCommands => s_handlers.Keys;

        public static JToken Dispatch(string command, JObject p)
        {
            if (!s_handlers.TryGetValue(command, out var handler))
                throw new InvalidOperationException($"Unknown command '{command}'. Use 'meta.list_commands' to see registered commands.");
            return handler(p) ?? JValue.CreateNull();
        }
    }
}
