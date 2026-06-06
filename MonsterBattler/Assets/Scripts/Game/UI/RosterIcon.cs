using MonsterBattler.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterBattler.Game.UI
{
    /// <summary>
    /// A read-only team-roster chip (PS shows six of these per side). Displays a teammate's
    /// species, tinted by its primary type, dimmed when fainted and outlined when it's the
    /// currently-active mon. Used for the opponent's roster row at the top of the screen.
    /// </summary>
    public sealed class RosterIcon : MonoBehaviour
    {
        [SerializeField] Image _background;
        [SerializeField] Text _label;
        [SerializeField] Outline _activeOutline; // optional; highlights the active mon

        public void Show(Pokemon mon, bool isActive)
        {
            if (mon == null) { gameObject.SetActive(false); return; }
            gameObject.SetActive(true);

            if (_label != null)
            {
                _label.text = Abbrev(mon.Species?.Name ?? mon.Nickname ?? "?");
                _label.color = mon.IsFainted ? new Color(1f, 1f, 1f, 0.45f) : Color.white;
            }
            if (_background != null)
            {
                var c = TypeStyle.BgColor(mon.Species != null ? mon.Species.Type1 : MonType.None);
                _background.color = mon.IsFainted
                    ? new Color(c.r * 0.30f, c.g * 0.30f, c.b * 0.30f, 0.85f)
                    : c;
            }
            if (_activeOutline != null) _activeOutline.enabled = isActive && !mon.IsFainted;
        }

        // Labels use best-fit shrinking, so we only hard-trim very long names.
        static string Abbrev(string name) => name.Length <= 12 ? name : name.Substring(0, 12);
    }
}
