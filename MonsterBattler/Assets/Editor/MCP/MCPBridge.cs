using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MonsterBattler.Editor.MCP
{
    [InitializeOnLoad]
    public static class MCPBridge
    {
        const int BasePort = 17984;

        static HttpListener s_listener;
        static Thread s_acceptThread;
        static CancellationTokenSource s_cts;
        static int s_port;
        static readonly ConcurrentQueue<PendingCall> s_mainThreadQueue = new();

        /// <summary>
        /// Port this instance listens on. Deterministic per project instance so several Unity
        /// editors (this project, other projects, ParrelSync clones) can run bridges side by side:
        /// preferred = 17984 (clone_N prefers 17985+N), walking forward if taken. EditorPrefs is
        /// machine-global so it must NOT be used. The bound port is written to
        /// Temp/MCPBridgePort.txt for discovery by tooling.
        /// </summary>
        public static int Port => s_port != 0 ? s_port : PreferredPort;

        static int PreferredPort
        {
            get
            {
                var projectRoot = Path.GetDirectoryName(Application.dataPath) ?? "";
                // ParrelSync clones are "<project>_clone_N" and contain a ".clone" marker file.
                var marker = Path.Combine(projectRoot, ".clone");
                if (File.Exists(marker) || projectRoot.Contains("_clone_"))
                {
                    var idx = projectRoot.LastIndexOf("_clone_", StringComparison.Ordinal);
                    int cloneIndex = 0;
                    if (idx >= 0) int.TryParse(projectRoot.Substring(idx + "_clone_".Length), out cloneIndex);
                    return BasePort + 1 + cloneIndex;
                }
                return BasePort;
            }
        }

        public static bool IsRunning => s_listener != null && s_listener.IsListening;

        static MCPBridge()
        {
            // Asset-import workers and batch processes also load editor assemblies; they must
            // not bind bridge ports (they'd shadow the real editors' ports).
            foreach (var a in Environment.GetCommandLineArgs())
                if (a == "-adb2" || a.StartsWith("AssetImportWorker", StringComparison.OrdinalIgnoreCase))
                    return;

            EditorApplication.update += PumpMainThread;
            EditorApplication.quitting += Stop;
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
            // Bind immediately. HttpListener doesn't need anything from the editor, and
            // EditorApplication.delayCall only fires once `update` pumps — which is paused
            // when the editor is backgrounded (e.g. another window has focus after a recompile).
            // PumpMainThread() below also self-heals if the listener ever drops.
            Start();
        }

        public static void Start()
        {
            if (IsRunning) return;
            // Try the preferred port first, then walk forward in case another editor instance
            // (this project or another) already holds it.
            int preferred = PreferredPort;
            for (int candidate = preferred; candidate < preferred + 16; candidate++)
            {
                try
                {
                    s_cts = new CancellationTokenSource();
                    s_listener = new HttpListener();
                    s_listener.Prefixes.Add($"http://127.0.0.1:{candidate}/");
                    s_listener.Start();
                    s_port = candidate;
                    WritePortFile(candidate);
                    s_acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "MCPBridge.Accept" };
                    s_acceptThread.Start();
                    Debug.Log($"[MCP] Bridge listening on http://127.0.0.1:{candidate}/");
                    return;
                }
                catch (Exception)
                {
                    try { s_listener?.Close(); } catch { }
                    s_listener = null;
                    s_cts = null;
                }
            }
            Debug.LogError($"[MCP] Failed to bind any port in {preferred}..{preferred + 15}");
        }

        static void WritePortFile(int port)
        {
            try
            {
                var projectRoot = Path.GetDirectoryName(Application.dataPath) ?? "";
                File.WriteAllText(Path.Combine(projectRoot, "Temp", "MCPBridgePort.txt"), port.ToString());
            }
            catch { /* Temp may not exist in exotic contexts; discovery just falls back */ }
        }

        public static void Stop()
        {
            try { s_cts?.Cancel(); } catch { }
            try { s_listener?.Stop(); } catch { }
            try { s_listener?.Close(); } catch { }
            s_listener = null;
            s_acceptThread = null;
            s_cts = null;
        }

        static void AcceptLoop()
        {
            var listener = s_listener;
            var token = s_cts.Token;
            while (!token.IsCancellationRequested && listener.IsListening)
            {
                HttpListenerContext ctx;
                try { ctx = listener.GetContext(); }
                catch { break; }
                ThreadPool.QueueUserWorkItem(_ => HandleContext(ctx));
            }
        }

        static void HandleContext(HttpListenerContext ctx)
        {
            string body;
            using (var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding ?? Encoding.UTF8))
                body = reader.ReadToEnd();

            string id = null;
            JObject responseJson;
            try
            {
                var request = JObject.Parse(string.IsNullOrEmpty(body) ? "{}" : body);
                id = (string)request["id"];
                var command = (string)request["command"];
                var paramsObj = request["params"] as JObject ?? new JObject();
                if (string.IsNullOrEmpty(command))
                    throw new ArgumentException("Missing 'command'");

                var tcs = new TaskCompletionSource<JToken>(TaskCreationOptions.RunContinuationsAsynchronously);
                s_mainThreadQueue.Enqueue(new PendingCall { Command = command, Params = paramsObj, Completion = tcs });
                var result = tcs.Task.GetAwaiter().GetResult();

                responseJson = new JObject { ["id"] = id, ["ok"] = true, ["result"] = result };
            }
            catch (Exception e)
            {
                responseJson = new JObject
                {
                    ["id"] = id,
                    ["ok"] = false,
                    ["error"] = new JObject
                    {
                        ["message"] = e.Message,
                        ["type"] = e.GetType().Name,
                        ["stack"] = e.StackTrace,
                    },
                };
            }

            var bytes = Encoding.UTF8.GetBytes(responseJson.ToString(Formatting.None));
            ctx.Response.ContentType = "application/json";
            ctx.Response.ContentLength64 = bytes.Length;
            try { ctx.Response.OutputStream.Write(bytes, 0, bytes.Length); }
            catch { }
            try { ctx.Response.OutputStream.Close(); }
            catch { }
        }

        static void PumpMainThread()
        {
            // Self-heal: if the listener dropped (e.g. due to a transient bind failure during
            // domain reload), try to bring it back up here. Cheap when already running.
            if (!IsRunning) Start();

            // Drain a bounded number per editor tick so we don't stall the editor.
            const int maxPerTick = 16;
            for (int i = 0; i < maxPerTick; i++)
            {
                if (!s_mainThreadQueue.TryDequeue(out var call)) break;
                try
                {
                    var result = MCPCommandRegistry.Dispatch(call.Command, call.Params);
                    call.Completion.SetResult(result);
                }
                catch (Exception e)
                {
                    call.Completion.SetException(e);
                }
            }
        }

        struct PendingCall
        {
            public string Command;
            public JObject Params;
            public TaskCompletionSource<JToken> Completion;
        }

        [MenuItem("MonsterBattler/MCP/Restart Bridge")]
        public static void RestartMenu()
        {
            Stop();
            Start();
        }

        [MenuItem("MonsterBattler/MCP/Status")]
        public static void StatusMenu()
        {
            Debug.Log($"[MCP] Running: {IsRunning}, Port: {Port}, Queue depth: {s_mainThreadQueue.Count}");
        }
    }
}
