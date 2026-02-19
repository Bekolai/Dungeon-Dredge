using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonDredge.Player
{
    public class LanternController : MonoBehaviour
    {
        private static readonly List<LanternController> ActiveLanterns = new List<LanternController>();
        private const string RuntimeLanternObjectName = "RuntimeLanternLight";

        [Header("Lantern")]
        [SerializeField] private Light lanternLight;
        [SerializeField] private bool startsEnabled = true;
        [SerializeField] private float repelRadius = 8f;
        [SerializeField] private Key defaultToggleKey = Key.L;

        [Header("Input")]
        [SerializeField] private InputActionReference toggleLanternAction;

        public bool IsLanternOn { get; private set; }
        public float RepelRadius => repelRadius;

        private void Awake()
        {
            if (lanternLight == null)
            {
                lanternLight = GetComponentInChildren<Light>();
            }

            if (lanternLight == null)
            {
                lanternLight = CreateRuntimeLanternLight();
            }

            SetLanternState(startsEnabled);
        }

        private void Update()
        {
            if (toggleLanternAction == null && Keyboard.current != null && Keyboard.current[defaultToggleKey].wasPressedThisFrame)
            {
                SetLanternState(!IsLanternOn);
            }
        }

        private void OnEnable()
        {
            if (!ActiveLanterns.Contains(this))
            {
                ActiveLanterns.Add(this);
            }

            if (toggleLanternAction != null)
            {
                toggleLanternAction.action.Enable();
                toggleLanternAction.action.performed += OnToggleLantern;
            }
        }

        private void OnDisable()
        {
            ActiveLanterns.Remove(this);

            if (toggleLanternAction != null)
            {
                toggleLanternAction.action.performed -= OnToggleLantern;
            }
        }

        private void OnToggleLantern(InputAction.CallbackContext context)
        {
            SetLanternState(!IsLanternOn);
        }

        public void SetLanternState(bool enabled)
        {
            IsLanternOn = enabled;
            if (lanternLight != null)
            {
                lanternLight.enabled = enabled;
            }
        }

        private Light CreateRuntimeLanternLight()
        {
            GameObject lightObject = new GameObject(RuntimeLanternObjectName);
            lightObject.transform.SetParent(transform);
            lightObject.transform.localPosition = new Vector3(0f, 1.55f, 0.35f);
            lightObject.transform.localRotation = Quaternion.identity;

            Light runtimeLight = lightObject.AddComponent<Light>();
            runtimeLight.type = LightType.Point;
            runtimeLight.range = Mathf.Max(7f, repelRadius);
            runtimeLight.intensity = 1.6f;
            runtimeLight.color = new Color(1f, 0.9f, 0.7f); // Warm lantern glow
            runtimeLight.shadows = LightShadows.Soft;
            runtimeLight.shadowStrength = 0.8f;
            runtimeLight.renderMode = LightRenderMode.ForcePixel;
            return runtimeLight;
        }

        public static bool TryGetRepellingLantern(Vector3 targetPosition, out Transform lanternOwner)
        {
            lanternOwner = null;

            foreach (var lantern in ActiveLanterns)
            {
                if (lantern == null || !lantern.IsLanternOn)
                    continue;

                float sqrDistance = (lantern.transform.position - targetPosition).sqrMagnitude;
                float sqrRadius = lantern.repelRadius * lantern.repelRadius;
                if (sqrDistance <= sqrRadius)
                {
                    lanternOwner = lantern.transform;
                    return true;
                }
            }

            return false;
        }
    }
}
