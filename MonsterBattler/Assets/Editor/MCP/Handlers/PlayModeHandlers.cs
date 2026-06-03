using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MonsterBattler.Editor.MCP.Handlers
{
    /// <summary>
    /// Play-mode lifecycle. Entering play mode is asynchronous in Unity — these commands
    /// flip the flag and return immediately; poll `playmode.state` to observe the transition.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeHandlers
    {
        static PlayModeHandlers()
        {
            MCPCommandRegistry.Register("playmode.state", _ => State());

            MCPCommandRegistry.Register("playmode.enter", _ =>
            {
                if (!EditorApplication.isPlaying)
                {
                    EditorApplication.isPlaying = true;
                }
                return State();
            });

            MCPCommandRegistry.Register("playmode.exit", _ =>
            {
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.isPlaying = false;
                }
                return State();
            });

            MCPCommandRegistry.Register("playmode.pause", _ =>
            {
                if (!EditorApplication.isPlaying)
                    throw new System.InvalidOperationException("Not in play mode");
                EditorApplication.isPaused = true;
                return State();
            });

            MCPCommandRegistry.Register("playmode.unpause", _ =>
            {
                if (!EditorApplication.isPlaying)
                    throw new System.InvalidOperationException("Not in play mode");
                EditorApplication.isPaused = false;
                return State();
            });

            MCPCommandRegistry.Register("playmode.step", _ =>
            {
                if (!EditorApplication.isPlaying || !EditorApplication.isPaused)
                    throw new System.InvalidOperationException("Step requires play mode + paused");
                EditorApplication.Step();
                return State();
            });
        }

        static JObject State() => new()
        {
            ["isPlaying"] = EditorApplication.isPlaying,
            ["isPlayingOrWillChange"] = EditorApplication.isPlayingOrWillChangePlaymode,
            ["isPaused"] = EditorApplication.isPaused,
            ["isCompiling"] = EditorApplication.isCompiling,
            ["isUpdating"] = EditorApplication.isUpdating,
            ["timeSinceStartup"] = EditorApplication.timeSinceStartup,
        };
    }
}
