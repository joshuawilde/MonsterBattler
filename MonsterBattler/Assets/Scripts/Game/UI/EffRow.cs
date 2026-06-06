using TMPro;
using UnityEngine;

namespace MonsterBattler.Game.UI
{
    /// <summary>
    /// One effectiveness row in the info panel: a multiplier label ("x2") followed by type-badge
    /// chips. The label is child 0; badge chips are instantiated after it (laid out by the row's
    /// HorizontalLayoutGroup). Hidden via SetActive when its bucket is empty.
    /// </summary>
    public sealed class EffRow : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _label;

        public void SetLabel(string text)
        {
            if (_label != null) _label.text = text;
        }

        /// <summary>Remove previously-added badge chips, keeping the label (child 0).</summary>
        public void ClearBadges()
        {
            for (int i = transform.childCount - 1; i >= 1; i--)
                Destroy(transform.GetChild(i).gameObject);
        }
    }
}
