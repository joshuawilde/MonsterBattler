using System;
using System.IO;
using MonsterBattler.Editor.MCP.Util;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MonsterBattler.Editor.MCP.Handlers
{
    /// <summary>
    /// Lets the agent see and drive the running game: capture the Game view to a PNG, and
    /// "click" UI buttons (so a whole turn can be played/verified from the CLI).
    /// </summary>
    [InitializeOnLoad]
    public static class InteractionHandlers
    {
        static InteractionHandlers()
        {
            // Capture the Game view to a PNG. Writes after the next rendered frame, so poll the
            // returned path for a non-empty file before reading. Best used in play mode.
            MCPCommandRegistry.Register("game.screenshot", p =>
            {
                var path = (string)p["path"];
                if (string.IsNullOrEmpty(path))
                    path = Path.Combine(Application.dataPath, "..", "Screenshots", "mcp.png");
                var full = Path.GetFullPath(path);
                Directory.CreateDirectory(Path.GetDirectoryName(full));
                if (File.Exists(full)) File.Delete(full); // so the caller can detect the fresh write
                int superSize = (int?)p["superSize"] ?? 1;
                ScreenCapture.CaptureScreenshot(full, Mathf.Max(1, superSize));
                return new JObject { ["path"] = full, ["note"] = "written after the next rendered frame" };
            });

            // Exact-frame capture: unlike game.screenshot (whenever the editor next renders), this
            // grabs end-of-frame via FrameRecorder, so the shot lands on THIS frame. Play mode only.
            MCPCommandRegistry.Register("game.snap", p =>
            {
                if (!Application.isPlaying) throw new InvalidOperationException("game.snap requires play mode");
                var full = Path.GetFullPath((string)p["path"]);
                Directory.CreateDirectory(Path.GetDirectoryName(full));
                MonsterBattler.Game.FrameRecorder.Snap(full);
                return new JObject { ["path"] = full };
            });

            // Record a PNG sequence (frame_0000.png…) every N frames — review animations as video.
            MCPCommandRegistry.Register("game.record_start", p =>
            {
                if (!Application.isPlaying) throw new InvalidOperationException("game.record_start requires play mode");
                var dir = Path.GetFullPath((string)p["dir"] ?? "/tmp/mb_rec");
                int everyN = (int?)p["everyN"] ?? 3;
                int max = (int?)p["max"] ?? 240;
                MonsterBattler.Game.FrameRecorder.StartRecording(dir, everyN, max);
                return new JObject { ["dir"] = dir, ["everyN"] = everyN, ["max"] = max };
            });

            MCPCommandRegistry.Register("game.record_stop", p => new JObject
            {
                ["dir"] = MonsterBattler.Game.FrameRecorder.StopRecording(),
                ["written"] = MonsterBattler.Game.FrameRecorder.Written,
            });

            MCPCommandRegistry.Register("game.record_status", p => new JObject
            {
                ["recording"] = MonsterBattler.Game.FrameRecorder.IsRecording,
                ["written"] = MonsterBattler.Game.FrameRecorder.Written,
            });

            // Fire a UI click on a GameObject (its Button/onClick + pointer handlers). Play mode only
            // — runtime listeners (wired in Awake/Start) aren't active in edit mode.
            MCPCommandRegistry.Register("ui.click", p =>
            {
                var go = GameObjectLookup.Resolve(p);
                bool handled = false;

                var ptr = new PointerEventData(EventSystem.current)
                {
                    button = PointerEventData.InputButton.Left,
                };
                if (ExecuteEvents.Execute(go, ptr, ExecuteEvents.pointerClickHandler)) handled = true;

                if (!handled)
                {
                    var btn = go.GetComponent<Button>();
                    if (btn != null && btn.IsActive() && btn.IsInteractable())
                    {
                        btn.onClick.Invoke();
                        handled = true;
                    }
                }
                return new JObject { ["clicked"] = handled, ["target"] = GameObjectLookup.PathOf(go) };
            });
        }
    }
}
