using MonsterBattler.Sim;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterBattler.Game.UI
{
    /// <summary>
    /// A small colored type chip (Showdown-style "FIRE" / "FIGHT" badge). Reused for a mon's types
    /// and for the info panel's weakness/resistance rows. Made into a prefab so all badges share one
    /// authored look; <see cref="SetType"/> recolors + relabels it per type.
    /// </summary>
    public sealed class TypeBadge : MonoBehaviour
    {
        [SerializeField] Image _background;
        [SerializeField] TextMeshProUGUI _label;

        /// <summary>Color + label this badge for a given type.</summary>
        public void SetType(MonType type)
        {
            if (_background != null) _background.color = TypeStyle.BgColor(type);
            if (_label != null) _label.text = TypeStyle.Display(type).ToUpperInvariant();
        }

        /// <summary>Generic setter (e.g. for a "×2" multiplier chip) — arbitrary label + bg color.</summary>
        public void Set(string label, Color color)
        {
            if (_background != null) _background.color = color;
            if (_label != null) _label.text = label;
        }

        /// <summary>Set the label + text color, keeping the prefab's styled background (for boost chips).</summary>
        public void SetChip(string label, Color textColor)
        {
            if (_label != null) { _label.text = label; _label.color = textColor; }
        }
    }
}
