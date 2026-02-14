using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonDredge.Core
{
    /// <summary>
    /// Simple event bus for decoupled communication between systems
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> eventHandlers = new();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            Type eventType = typeof(T);
            if (!eventHandlers.ContainsKey(eventType))
            {
                eventHandlers[eventType] = new List<Delegate>();
            }
            eventHandlers[eventType].Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            Type eventType = typeof(T);
            if (eventHandlers.ContainsKey(eventType))
            {
                eventHandlers[eventType].Remove(handler);
            }
        }

        public static void Publish<T>(T eventData) where T : struct
        {
            Type eventType = typeof(T);
            if (eventHandlers.ContainsKey(eventType))
            {
                foreach (var handler in eventHandlers[eventType])
                {
                    ((Action<T>)handler)?.Invoke(eventData);
                }
            }
        }

        public static void Clear()
        {
            eventHandlers.Clear();
        }
    }

    // Game Events
    public struct NoiseEvent
    {
        public Vector3 Position;
        public float Intensity;
        public GameObject Source;
    }

    public struct EncumbranceChangedEvent
    {
        public float WeightRatio;
        public EncumbranceTier Tier;
    }

    public struct StaminaChangedEvent
    {
        public float CurrentStamina;
        public float MaxStamina;
        public float Ratio;
    }

    public struct ItemPickedUpEvent
    {
        public string ItemId;
        public float Weight;
    }

    public struct ItemDroppedEvent
    {
        public string ItemId;
        public float Weight;
    }

    public struct PlayerStatChangedEvent
    {
        public StatType StatType;
        public int NewLevel;
        public float CurrentXP;
    }

    public struct QuestProgressEvent
    {
        public string QuestId;
        public string ObjectiveId;
        public int CurrentProgress;
        public int RequiredProgress;
    }

    public struct EnemyAlertedEvent
    {
        public GameObject Enemy;
        public Vector3 LastKnownPlayerPosition;
    }

    public struct ExtractionStartedEvent
    {
        public float Duration;
    }

    public struct ExtractionCompletedEvent
    {
        public int TotalGold;
        public int ItemCount;
    }

    public struct InventoryFeedbackEvent
    {
        public string Message;
        public float Duration;
        public bool IsWarning;
    }

    public enum StatType
    {
        Strength,
        Endurance,
        Perception
    }

    public enum EncumbranceTier
    {
        Light,      // < 40%
        Medium,     // 40-100%
        Heavy,      // 100-120%
        Snail       // 120-140%
    }
}
