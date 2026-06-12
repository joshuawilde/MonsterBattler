using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace MonsterBattler.Editor.MCP.Handlers
{
    /// <summary>Player builds from the CLI — used for the Linux dedicated server (Rivet).</summary>
    [InitializeOnLoad]
    public static class BuildHandlers
    {
        static BuildHandlers()
        {
            // Build the Linux dedicated server. params: path (default Builds/LinuxServer)
            // SLOW (minutes; switches build target for the duration) — call with a long timeout.
            MCPCommandRegistry.Register("build.server", p =>
            {
                var dir = (string)p["path"] ?? "Builds/LinuxServer";
                var options = new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    locationPathName = Path.Combine(dir, "MonsterBattler.x86_64"),
                    target = BuildTarget.StandaloneLinux64,
                    subtarget = (int)StandaloneBuildSubtarget.Server,
                    options = BuildOptions.None,
                };
                BuildReport report = BuildPipeline.BuildPlayer(options);
                return new JObject
                {
                    ["result"] = report.summary.result.ToString(),
                    ["totalSizeMB"] = report.summary.totalSize / (1024 * 1024),
                    ["errors"] = report.summary.totalErrors,
                    ["output"] = report.summary.outputPath,
                };
            });
        }
    }
}
