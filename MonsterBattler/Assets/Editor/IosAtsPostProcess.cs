#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace MonsterBattler.Editor
{
    /// <summary>
    /// Injects a scoped App Transport Security exception into the generated iOS Info.plist so the
    /// app can reach the battle server over plain ws:// (Unity's Mono TLS can't complete a wss
    /// handshake). Scoped to the VPS host only — Apple no longer accepts a blanket
    /// NSAllowsArbitraryLoads without a review justification, and all other traffic (the Go backend
    /// on the same host) already uses HTTPS, which this exception leaves untouched.
    /// </summary>
    public static class IosAtsPostProcess
    {
        // The one host we hit over insecure ws (port-agnostic — ATS keys on the domain, not port).
        const string BattleHost = "vps-7d32ac6f.vps.ovh.us";

        [PostProcessBuild(100)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS) return;

            string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            PlistElementDict root = plist.root;

            PlistElementDict ats = root.values.ContainsKey("NSAppTransportSecurity")
                ? root["NSAppTransportSecurity"].AsDict()
                : root.CreateDict("NSAppTransportSecurity");

            PlistElementDict domains = ats.values.ContainsKey("NSExceptionDomains")
                ? ats["NSExceptionDomains"].AsDict()
                : ats.CreateDict("NSExceptionDomains");

            PlistElementDict host = domains.values.ContainsKey(BattleHost)
                ? domains[BattleHost].AsDict()
                : domains.CreateDict(BattleHost);

            // Permit plain ws/http to this host only. HTTPS to the same host still negotiates TLS.
            host.SetBoolean("NSExceptionAllowsInsecureHTTPLoads", true);
            host.SetBoolean("NSIncludesSubdomains", false);

            plist.WriteToFile(plistPath);
            Debug.Log($"[iOS ATS] added scoped insecure-load exception for {BattleHost} (plain ws battle relay)");
        }
    }
}
#endif
