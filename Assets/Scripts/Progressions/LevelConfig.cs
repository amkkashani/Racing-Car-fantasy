using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "LevelConfig", menuName = "Progression/LevelConfig")]
public class LevelConfig : ScriptableObject
{
    [Serializable]
    public class LevelTier
    {
        [Min(1)] public int levelNumber = 1;
        [Min(0f)] public float minKm = 0f;

        [Header("Display")] [Tooltip("Shown in UI together with the number, e.g. 'Rookie', 'Pro', 'Master'.")]
        public string levelName = "Rookie";

        [Header("Sprites (optional)")] public Sprite badgeSprite; // can be null
        public Sprite avatarSprite; // can be null
        [Header("Environment Settings (optional)")]
        public EnvSettings env;   // if env.isDefined==false, this tier doesn't override
    }

    [Tooltip("Sorted by minKm ascending.")]
    public List<LevelTier> tiers = new()
    {
        new LevelTier { levelNumber = 1, minKm = 0, levelName = "Rookie" },
        new LevelTier { levelNumber = 2, minKm = 1, levelName = "Driver" },
        new LevelTier { levelNumber = 3, minKm = 2, levelName = "Racer" },
        new LevelTier { levelNumber = 4, minKm = 4, levelName = "Pro" },
        new LevelTier { levelNumber = 5, minKm = 8, levelName = "Legend" },
        new LevelTier { levelNumber = 6, minKm = 10, levelName = "Sprinter" },
        new LevelTier { levelNumber = 7, minKm = 15, levelName = "Cruiser" },
        new LevelTier { levelNumber = 8, minKm = 20, levelName = "Speedster" },
        new LevelTier { levelNumber = 9, minKm = 25, levelName = "Challenger" },
        new LevelTier { levelNumber = 10, minKm = 30, levelName = "Striker" },
        new LevelTier { levelNumber = 11, minKm = 35, levelName = "Pursuer" },
        new LevelTier { levelNumber = 12, minKm = 40, levelName = "Trailblazer" },
        new LevelTier { levelNumber = 13, minKm = 45, levelName = "Swift Rider" },
        new LevelTier { levelNumber = 14, minKm = 50, levelName = "Jetstream" },
        new LevelTier { levelNumber = 15, minKm = 55, levelName = "Turbo" },
        new LevelTier { levelNumber = 16, minKm = 60, levelName = "Storm Rider" },
        new LevelTier { levelNumber = 17, minKm = 65, levelName = "Drifter" },
        new LevelTier { levelNumber = 18, minKm = 70, levelName = "Nitro" },
        new LevelTier { levelNumber = 19, minKm = 75, levelName = "Vortex" },
        new LevelTier { levelNumber = 20, minKm = 80, levelName = "Accelerator" },
        new LevelTier { levelNumber = 21, minKm = 85, levelName = "Overtaker" },
        new LevelTier { levelNumber = 22, minKm = 90, levelName = "Heatseeker" },
        new LevelTier { levelNumber = 23, minKm = 95, levelName = "Road Warrior" },
        new LevelTier { levelNumber = 24, minKm = 100, levelName = "Speed Demon" },
        new LevelTier { levelNumber = 25, minKm = 110, levelName = "Highway Star" },
        new LevelTier { levelNumber = 26, minKm = 120, levelName = "Iron Driver" },
        new LevelTier { levelNumber = 27, minKm = 130, levelName = "Track Master" },
        new LevelTier { levelNumber = 28, minKm = 140, levelName = "Velocity" },
        new LevelTier { levelNumber = 29, minKm = 150, levelName = "Supersonic" },
        new LevelTier { levelNumber = 30, minKm = 160, levelName = "Phantom Rider" },
        new LevelTier { levelNumber = 31, minKm = 170, levelName = "Dominant Force" },
        new LevelTier { levelNumber = 32, minKm = 180, levelName = "Mach Breaker" },
        new LevelTier { levelNumber = 33, minKm = 190, levelName = "Overdrive" },
        new LevelTier { levelNumber = 34, minKm = 200, levelName = "Track Titan" },
        new LevelTier { levelNumber = 35, minKm = 210, levelName = "Inferno" },
        new LevelTier { levelNumber = 36, minKm = 220, levelName = "Warp Rider" },
        new LevelTier { levelNumber = 37, minKm = 230, levelName = "Gravity Bender" },
        new LevelTier { levelNumber = 38, minKm = 240, levelName = "Eclipse" },
        new LevelTier { levelNumber = 39, minKm = 250, levelName = "Apex Predator" },
        new LevelTier { levelNumber = 40, minKm = 260, levelName = "Immortal Legend" },
    };

    /// Return best tier for KM (last tier whose minKm <= totalKm).
    public LevelTier GetTierForKm(double totalKm)
    {
        if (tiers == null || tiers.Count == 0) return null;
        LevelTier result = tiers[0];
        for (int i = 0; i < tiers.Count; i++)
        {
            if (totalKm >= tiers[i].minKm) result = tiers[i];
            else break;
        }

        return result;
    }
    
    

    /// Resolve display payload with sprite fallbacks:
    /// - If a sprite is null for current tier, walk backward to the closest earlier tier that has it.
    public void ResolveDisplayForKm(
        double totalKm,
        out int levelNumber,
        out string levelName,
        out Sprite resolvedBadge,
        out Sprite resolvedAvatar)
    {
        resolvedBadge = null;
        resolvedAvatar = null;
        levelNumber = 1;
        levelName = "Level 1";

        if (tiers == null || tiers.Count == 0) return;

        // Current tier index
        int idx = 0;
        for (int i = 0; i < tiers.Count; i++)
        {
            if (totalKm >= tiers[i].minKm) idx = i;
            else break;
        }

        var current = tiers[idx];
        levelNumber = Math.Max(1, current.levelNumber);
        levelName = string.IsNullOrEmpty(current.levelName) ? $"Level {levelNumber}" : current.levelName;

        // Walk backward to find the closest non-null sprites
        for (int i = idx; i >= 0 && (resolvedBadge == null || resolvedAvatar == null); i--)
        {
            var t = tiers[i];
            if (resolvedBadge == null && t.badgeSprite != null) resolvedBadge = t.badgeSprite;
            if (resolvedAvatar == null && t.avatarSprite != null) resolvedAvatar = t.avatarSprite;
        }
    }

    public bool TryGetNextTier(double totalKm, out LevelTier next)
    {
        next = null;
        if (tiers == null || tiers.Count == 0) return false;

        // Find the first tier whose minKm is strictly greater than current progress.
        for (int i = 0; i < tiers.Count; i++)
        {
            if (totalKm < tiers[i].minKm)
            {
                next = tiers[i];
                return true;
            }
        }

        return false; // already at/above highest tier
    }

    /// Returns the index of the tier whose minKm <= totalKm (current tier).
    public int GetTierIndexForKm(double totalKm)
    {
        if (tiers == null || tiers.Count == 0) return -1;
        int idx = 0;
        for (int i = 0; i < tiers.Count; i++)
        {
            if (totalKm >= tiers[i].minKm) idx = i;
            else break;
        }
        return idx;
    }
    
    
    
    /// Resolve environment settings for a given KM:
    /// - If the current tier's EnvSettings.isDefined is false,
    ///   walk backward to the closest earlier tier where it is true.
    /// Returns true if something was found; false if nothing defined in any earlier tier.
    public bool ResolveEnvSettingsForKm(double totalKm, out EnvSettings settings, out int resolvedTierIndex)
    {
        settings = default;
        resolvedTierIndex = -1;

        if (tiers == null || tiers.Count == 0) return false;

        int idx = GetTierIndexForKm(totalKm);
        if (idx < 0) return false;

        for (int i = idx; i >= 0; i--)
        {
            if (tiers[i].env.isDefined)
            {
                settings = tiers[i].env;
                resolvedTierIndex = i;
                return true;
            }
        }
        return false;
    }
    

    public bool TryGetPrevTier(double totalKm, out LevelTier prev)
    {
        prev = null;
        int idx = GetTierIndexForKm(totalKm);
        if (idx <= 0) return false;
        prev = tiers[idx - 1];
        return true;
    }


#if UNITY_EDITOR
    [ContextMenu("Validate & Sort")]
    private void ValidateAndSort()
    {
        tiers.Sort((a, b) => a.minKm.CompareTo(b.minKm));
        for (int i = 0; i < tiers.Count; i++)
        {
            if (tiers[i].levelNumber <= 0) tiers[i].levelNumber = i + 1;
            if (string.IsNullOrWhiteSpace(tiers[i].levelName))
                tiers[i].levelName = $"Level {tiers[i].levelNumber}";
            if (i > 0 && tiers[i].minKm < tiers[i - 1].minKm)
                tiers[i].minKm = tiers[i - 1].minKm;
        }

        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}

[Serializable]
public struct EnvSettings
{
    public bool isDefined;          // mark if this tier actually defines settings
    [Header("Lighting / Time of Day")]
    public bool isNight;            // false = day, true = night

    [Header("Visuals (optional)")]
    [Range(0f, 1f)] public float fogDensity;
    public Color fogColor;

    [Header("Traffic / Gameplay (optional)")]
    [Range(0f, 1f)] public float trafficDensity;
    [Range(0f, 1f)] public float rainAmount;

    // add any other simple fields you need; keep it serializable-friendly
}