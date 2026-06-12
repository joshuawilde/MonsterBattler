using System.IO;
using UnityEngine;

namespace MonsterBattler.Game
{
    /// <summary>
    /// In-game frame capture for verification: <see cref="Snap"/> grabs EXACTLY the current frame
    /// (end-of-frame, after UI renders) — callable from any game code at the precise moment that
    /// matters — and <see cref="StartRecording"/> dumps a numbered PNG sequence every N frames so
    /// animations can be reviewed as a video/contact-sheet. Driven from code or the MCP bridge
    /// (game.snap / game.record_start / game.record_stop). Play mode only.
    /// </summary>
    public sealed class FrameRecorder : MonoBehaviour
    {
        static FrameRecorder _inst;

        string _dir;
        int _everyN = 3, _frame, _written, _max;
        bool _recording;

        public static bool IsRecording => _inst != null && _inst._recording;
        public static int Written => _inst != null ? _inst._written : 0;

        static FrameRecorder Instance()
        {
            if (_inst == null)
            {
                var go = new GameObject("[FrameRecorder]");
                DontDestroyOnLoad(go);
                _inst = go.AddComponent<FrameRecorder>();
            }
            return _inst;
        }

        /// <summary>Capture exactly this frame to a PNG (after UI/particles render).</summary>
        public static void Snap(string path) => Instance().StartCoroutine(Instance().SnapEndOfFrame(path));

        /// <summary>Begin dumping frame_0000.png… into dir, one shot every N frames.</summary>
        public static void StartRecording(string dir, int everyN = 3, int maxFrames = 240)
        {
            var r = Instance();
            Directory.CreateDirectory(dir);
            r._dir = dir;
            r._everyN = Mathf.Max(1, everyN);
            r._max = Mathf.Max(1, maxFrames);
            r._frame = 0;
            r._written = 0;
            r._recording = true;
        }

        /// <summary>Stop and return the recording dir (null if never started).</summary>
        public static string StopRecording()
        {
            if (_inst == null) return null;
            _inst._recording = false;
            return _inst._dir;
        }

        void LateUpdate()
        {
            if (!_recording) return;
            if (_written >= _max) { _recording = false; return; }
            if (_frame++ % _everyN != 0) return;
            StartCoroutine(SnapEndOfFrame(Path.Combine(_dir, $"frame_{_written++:D4}.png")));
        }

        System.Collections.IEnumerator SnapEndOfFrame(string path)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            try { File.WriteAllBytes(path, tex.EncodeToPNG()); }
            finally { Destroy(tex); }
        }
    }
}
