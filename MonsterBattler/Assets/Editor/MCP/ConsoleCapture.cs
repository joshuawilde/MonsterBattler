using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MonsterBattler.Editor.MCP
{
    /// <summary>
    /// Thread-safe ring buffer of Unity console log entries. Subscribes once at editor load
    /// to <see cref="Application.logMessageReceivedThreaded"/>, which fires for every Debug.Log /
    /// uncaught exception in both edit mode and play mode.
    ///
    /// Compile errors emitted by the C# compiler don't come through here — they appear via the
    /// internal LogEntries API. We rely on Debug.LogError from user code for now; we can wire
    /// LogEntries via reflection later if compile-error visibility becomes necessary.
    /// </summary>
    [InitializeOnLoad]
    public static class ConsoleCapture
    {
        public sealed class Entry
        {
            public long Seq;
            public DateTime TimeUtc;
            public LogType Type;
            public string Message;
            public string Stack;
        }

        const int Capacity = 2000;
        static readonly object s_lock = new();
        static readonly LinkedList<Entry> s_entries = new();
        static long s_seq;

        static ConsoleCapture()
        {
            Application.logMessageReceivedThreaded -= OnLog;
            Application.logMessageReceivedThreaded += OnLog;
            AssemblyReloadEvents.beforeAssemblyReload += () =>
                Application.logMessageReceivedThreaded -= OnLog;
        }

        static void OnLog(string message, string stackTrace, LogType type)
        {
            var e = new Entry
            {
                Seq = System.Threading.Interlocked.Increment(ref s_seq),
                TimeUtc = DateTime.UtcNow,
                Type = type,
                Message = message,
                Stack = stackTrace,
            };
            lock (s_lock)
            {
                s_entries.AddLast(e);
                while (s_entries.Count > Capacity) s_entries.RemoveFirst();
            }
        }

        public static List<Entry> Snapshot()
        {
            lock (s_lock) return new List<Entry>(s_entries);
        }

        public static int Clear()
        {
            lock (s_lock)
            {
                var n = s_entries.Count;
                s_entries.Clear();
                return n;
            }
        }

        public static (int log, int warn, int error, int assert, int exception) CountBySeverity()
        {
            int l = 0, w = 0, e = 0, a = 0, x = 0;
            lock (s_lock)
            {
                foreach (var entry in s_entries)
                {
                    switch (entry.Type)
                    {
                        case LogType.Log: l++; break;
                        case LogType.Warning: w++; break;
                        case LogType.Error: e++; break;
                        case LogType.Assert: a++; break;
                        case LogType.Exception: x++; break;
                    }
                }
            }
            return (l, w, e, a, x);
        }
    }
}
