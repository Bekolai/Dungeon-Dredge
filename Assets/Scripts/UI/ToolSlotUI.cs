using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DungeonDredge.Tools;

namespace DungeonDredge.UI
{
    public class ToolSlotUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI chargesText;
        [SerializeField] private TextMeshProUGUI keyText;
        [SerializeField] private Image cooldownOverlay;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color selectedColor = new Color(0.4f, 0.4f, 0.2f, 0.8f);
        [SerializeField] private Color emptyColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);

        [Header("Settings")]
        [SerializeField] private int slotNumber = 1;

        private ToolBase currentTool;
        private bool isSelected;

        private void Start()
        {
            if (keyText != null)
            {
                keyText.text = slotNumber.ToString();
            }
        }

        private void Update()
        {
            UpdateCooldown();
        }

        public void UpdateSlot(ToolBase tool, bool selected)
        {
            currentTool = tool;
            isSelected = selected;

            if (tool == null)
            {
                // Empty slot
                if (iconImage != null)
                {
                    iconImage.enabled = false;
                }
                if (chargesText != null)
                {
                    chargesText.text = "";
                }
                if (backgroundImage != null)
                {
                    backgroundImage.color = emptyColor;
                }
                if (cooldownOverlay != null)
                {
                    cooldownOverlay.fillAmount = 0f;
                }
            }
            else
            {
                // Has tool
                if (iconImage != null)
                {
                    iconImage.enabled = true;
                    iconImage.sprite = tool.Icon;
                }
                if (chargesText != null)
                {
                    chargesText.text = tool.CurrentCharges.ToString();
                    chargesText.color = tool.CurrentCharges > 0 ? Color.white : Color.red;
                }
                if (backgroundImage != null)
                {
                    backgroundImage.color = selected ? selectedColor : normalColor;
                }
            }
        }

        private void UpdateCooldown()
        {
            if (cooldownOverlay == null || currentTool == null) return;

            if (currentTool.IsOnCooldown)
            {
                cooldownOverlay.fillAmount = 1f - currentTool.CooldownProgress;
            }
            else
            {
                cooldownOverlay.fillAmount = 0f;
            }
        }
    }
}
