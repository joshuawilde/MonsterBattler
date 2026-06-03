using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MonsterBattler.Editor.MCP.Handlers
{
    [InitializeOnLoad]
    public static class ConsoleHandlers
    {
        static ConsoleHandlers()
        {
            MCPCommandRegistry.Register("console.get_logs", p =>
            {
                int tail = p?["tail"]?.Value<int>() ?? 100;
                long? sinceSeq = p?["sinceSeq"]?.Value<long?>();
                var includeStack = p?["includeStack"]?.Value<bool>() ?? false;

                var severityFilter = ParseSeverityFilter(p?["severity"]);
                var pattern = (string)p?["pattern"];
                Regex re = string.IsNullOrEmpty(pattern) ? null : new Regex(pattern, RegexOptions.IgnoreCase);

                var snap = ConsoleCapture.Snapshot();
                IEnumerable<ConsoleCapture.Entry> filtered = snap;
                if (sinceSeq.HasValue) filtered = filtered.Where(e => e.Seq > sinceSeq.Value);
                if (severityFilter != null) filtered = filtered.Where(e => severityFilter.Contains(e.Type));
                if (re != null) filtered = filtered.Where(e => re.IsMatch(e.Message ?? ""));

                var list = filtered.ToList();
                if (tail > 0 && list.Count > tail) list = list.GetRange(list.Count - tail, tail);

                var arr = new JArray();
                foreach (var e in list)
                {
                    var entry = new JObject
                    {
                        ["seq"] = e.Seq,
                        ["timeUtc"] = e.TimeUtc.ToString("o"),
                        ["severity"] = e.Type.ToString(),
                        ["message"] = e.Message,
                    };
                    if (includeStack) entry["stack"] = e.Stack;
                    arr.Add(entry);
                }
                return new JObject { ["entries"] = arr, ["totalCaptured"] = snap.Count };
            });

            MCPCommandRegistry.Register("console.count", _ =>
            {
                var (l, w, e, a, x) = ConsoleCapture.CountBySeverity();
                return new JObject
                {
                    ["log"] = l, ["warning"] = w, ["error"] = e, ["assert"] = a, ["exception"] = x,
                    ["total"] = l + w + e + a + x,
                };
            });

            MCPCommandRegistry.Register("console.clear", _ =>
                new JObject { ["cleared"] = ConsoleCapture.Clear() });

            MCPCommandRegistry.Register("console.last_error", p =>
            {
                bool includeStack = p?["includeStack"]?.Value<bool>() ?? true;
                var snap = ConsoleCapture.Snapshot();
                for (int i = snap.Count - 1; i >= 0; i--)
                {
                    var e = snap[i];
                    if (e.Type == LogType.Error || e.Type == LogType.Exception || e.Type == LogType.Assert)
                    {
                        var o = new JObject
                        {
                            ["seq"] = e.Seq,
                            ["timeUtc"] = e.TimeUtc.ToString("o"),
                            ["severity"] = e.Type.ToString(),
                            ["message"] = e.Message,
                        };
                        if (includeStack) o["stack"] = e.Stack;
                        return o;
                    }
                }
                return JValue.CreateNull();
            });
        }

        static HashSet<LogType> ParseSeverityFilter(JToken t)
        {
            if (t == null || t.Type == JTokenType.Null) return null;
            var set = new HashSet<LogType>();
            if (t is JArray arr)
                foreach (var item in arr) AddSeverity(set, (string)item);
            else
                AddSeverity(set, (string)t);
            return set.Count == 0 ? null : set;
        }

        static void AddSeverity(HashSet<LogType> set, string s)
        {
            if (string.IsNullOrEmpty(s)) return;
            if (Enum.TryParse<LogType>(s, ignoreCase: true, out var t)) set.Add(t);
            else throw new ArgumentException($"Unknown severity '{s}'. Use Log/Warning/Error/Assert/Exception.");
        }
    }
}
