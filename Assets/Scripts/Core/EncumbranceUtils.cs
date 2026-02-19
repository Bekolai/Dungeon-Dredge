using UnityEngine;

namespace DungeonDredge.Core
{
    public static class EncumbranceUtils
    {
        public const float LightThreshold = 0.4f;      // < 40%
        public const float NormalThreshold = 1.0f;     // 40-100%
        public const float HeavyThreshold = 1.2f;      // 100-120%
        public const float MaxOverloadThreshold = 1.4f; // 120-140% (max)

        public static EncumbranceTier GetTier(float ratio)
        {
            if (ratio < LightThreshold)
                return EncumbranceTier.Light;
            if (ratio < NormalThreshold)
                return EncumbranceTier.Medium;
            if (ratio < HeavyThreshold)
                return EncumbranceTier.Heavy;
            return EncumbranceTier.Snail;
        }

        public static string GetTierName(EncumbranceTier tier)
        {
            return tier switch
            {
                EncumbranceTier.Light => "Light",
                EncumbranceTier.Medium => "Medium",
                EncumbranceTier.Heavy => "Heavy",
                EncumbranceTier.Snail => "Overloaded",
                _ => "Light"
            };
        }
    }
}
