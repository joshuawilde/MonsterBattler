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
        const string PortPrefKey = "MonsterBattler.MCP.Port";
        const int DefaultPort = 17984;

        static HttpListener s_listener;
        static Thread s_acceptThread;
        static CancellationTokenSource s_cts;
        static readonly ConcurrentQueue<PendingCall> s_mainThreadQueue = new();

        public static int Port => EditorPrefs.GetInt(PortPrefKey, DefaultPort);
        public static bool IsRunning => s_listener != null && s_listener.IsListening;

        static MCPBridge()
        {
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
            try
            {
                s_cts = new CancellationTokenSource();
                s_listener = new HttpListener();
                s_listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
                s_listener.Start();
                s_acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "MCPBridge.Accept" };
                s_acceptThread.Start();
                Debug.Log($"[MCP] Bridge listening on http://127.0.0.1:{Port}/");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MCP] Failed to start bridge: {e.Message}");
                Stop();
            }
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
