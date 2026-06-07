using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MonsterBattler.Game.UI
{
    /// <summary>
    /// Showdown-style battle message box. Each action (a move, a switch, …) is one GROUP: the box
    /// fades in showing the action line, then its effect lines stack underneath and the box grows
    /// vertically (it only ever shows the CURRENT action's lines). When the action ends the box fades
    /// out, and the next action fades a fresh box back in. Height is driven by a ContentSizeFitter.
    /// </summary>
    public sealed class MessageBar : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _text;
        [SerializeField] CanvasGroup _group;
        [SerializeField] float _fade = 0.3f;

        readonly List<string> _lines = new List<string>();
        float _alpha;
        int _dir; // +1 fading in, -1 fading out, 0 idle

        void Awake()
        {
            _alpha = 0f;
            if (_group != null) _group.alpha = 0f;
        }

        /// <summary>Start a fresh box for a new action (clears prior lines) and fade it in.</summary>
        public void BeginGroup(string line)
        {
            _lines.Clear();
            if (!string.IsNullOrEmpty(line)) _lines.Add(line);
            Apply();
            _dir = 1;
        }

        /// <summary>Add an effect line under the current action (box grows).</summary>
        public void AppendLine(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            _lines.Add(line);
            Apply();
            if (_alpha < 1f) _dir = 1;
        }

        /// <summary>Fade the current box out (action finished).</summary>
        public void FadeOut()
        {
            if (_lines.Count > 0 || _alpha > 0f) _dir = -1;
        }

        void Apply()
        {
            if (_text != null) _text.text = string.Join("\n", _lines);
        }

        void Update()
        {
            if (_dir == 0) return;
            _alpha += _dir * Time.deltaTime / Mathf.Max(0.01f, _fade);
            if (_alpha >= 1f) { _alpha = 1f; _dir = 0; }
            else if (_alpha <= 0f) { _alpha = 0f; _dir = 0; _lines.Clear(); Apply(); }
            if (_group != null) _group.alpha = _alpha;
        }
    }
}
