using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace DungeonDredge.Tools
{
    public class ToolManager : MonoBehaviour
    {
        [Header("Tool Slots")]
        [SerializeField] private ToolBase[] toolSlots = new ToolBase[4];
        [SerializeField] private int currentSlot = 0;

        [Header("Input")]
        [SerializeField] private InputActionReference primaryAction;
        [SerializeField] private InputActionReference secondaryAction;
        [SerializeField] private InputActionReference slot1Action;
        [SerializeField] private InputActionReference slot2Action;
        [SerializeField] private InputActionReference slot3Action;
        [SerializeField] private InputActionReference slot4Action;

        [Header("Visual")]
        [SerializeField] private Transform toolHolder;

        // Properties
        public ToolBase CurrentTool => currentSlot >= 0 && currentSlot < toolSlots.Length ? toolSlots[currentSlot] : null;
        public ToolBase[] AllTools => toolSlots;
        public int CurrentSlot => currentSlot;

        // Events
        public System.Action<int> OnSlotChanged;
        public System.Action<ToolBase> OnToolUsed;

        private void OnEnable()
        {
            BindInput(primaryAction, OnPrimaryUse);
            BindInput(secondaryAction, OnSecondaryUse);
            BindInput(slot1Action, () => SelectSlot(0));
            BindInput(slot2Action, () => SelectSlot(1));
            BindInput(slot3Action, () => SelectSlot(2));
            BindInput(slot4Action, () => SelectSlot(3));
        }

        private void OnDisable()
        {
            UnbindInput(primaryAction, OnPrimaryUse);
            UnbindInput(secondaryAction, OnSecondaryUse);
            UnbindInput(slot1Action, () => SelectSlot(0));
            UnbindInput(slot2Action, () => SelectSlot(1));
            UnbindInput(slot3Action, () => SelectSlot(2));
            UnbindInput(slot4Action, () => SelectSlot(3));
        }

        private void BindInput(InputActionReference actionRef, System.Action callback)
        {
            if (actionRef != null)
            {
                actionRef.action.Enable();
                actionRef.action.performed += ctx => callback();
            }
        }

        private void UnbindInput(InputActionReference actionRef, System.Action callback)
        {
            if (actionRef != null)
            {
                actionRef.action.performed -= ctx => callback();
            }
        }

        public void SelectSlot(int slot)
        {
            if (slot < 0 || slot >= toolSlots.Length) return;
            if (slot == currentSlot) return;

            // Deactivate current tool visual
            if (CurrentTool != null)
            {
                CurrentTool.gameObject.SetActive(false);
            }

            currentSlot = slot;

            // Activate new tool visual
            if (CurrentTool != null)
            {
                CurrentTool.gameObject.SetActive(true);
            }

            OnSlotChanged?.Invoke(currentSlot);
        }

        private void OnPrimaryUse()
        {
            if (CurrentTool != null)
            {
                CurrentTool.TryUsePrimary();
                OnToolUsed?.Invoke(CurrentTool);
            }
        }

        private void OnSecondaryUse()
        {
            if (CurrentTool != null)
            {
                CurrentTool.TryUseSecondary();
            }
        }

        public void EquipTool(ToolBase tool, int slot)
        {
            if (slot < 0 || slot >= toolSlots.Length) return;

            // Remove existing tool
            if (toolSlots[slot] != null)
            {
                Destroy(toolSlots[slot].gameObject);
            }

            // Equip new tool
            toolSlots[slot] = tool;
            tool.transform.SetParent(toolHolder);
            tool.transform.localPosition = Vector3.zero;
            tool.transform.localRotation = Quaternion.identity;

            // Hide if not current slot
            tool.gameObject.SetActive(slot == currentSlot);
        }

        public void RemoveTool(int slot)
        {
            if (slot < 0 || slot >= toolSlots.Length) return;
            if (toolSlots[slot] == null) return;

            Destroy(toolSlots[slot].gameObject);
            toolSlots[slot] = null;
        }

        public bool HasTool(int slot)
        {
            return slot >= 0 && slot < toolSlots.Length && toolSlots[slot] != null;
        }

        public void RefillAllTools()
        {
            foreach (var tool in toolSlots)
            {
                tool?.Refill();
            }
        }
    }
}
