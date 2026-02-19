using UnityEngine;
using UnityEngine.EventSystems;

namespace DungeonDredge.UI
{
    /// <summary>
    /// A reusable trigger for generic tooltips.
    /// Can be attached to any UI element to show a tooltip on hover.
    /// </summary>
    public class StatTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Content")]
        public string title;
        [TextArea(3, 10)]
        public string description;
        public Color titleColor = Color.white;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (ItemTooltipUI.Instance != null)
            {
                ItemTooltipUI.Instance.ShowGeneric(title, description, titleColor);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (ItemTooltipUI.Instance != null)
            {
                ItemTooltipUI.Instance.Hide();
            }
        }

        private void OnDisable()
        {
            // Ensure tooltip hides if the element is disabled (e.g. panel closed)
            if (ItemTooltipUI.Instance != null)
            {
                ItemTooltipUI.Instance.Hide();
            }
        }
    }
}
