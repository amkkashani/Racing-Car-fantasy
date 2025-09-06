using System;
using System.IO;
using Racing2D;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class ProgressionSystem : SingletonMagic<ProgressionSystem>
{
    [Header("Config")] [SerializeField] private LevelConfig levelConfig; // assign your LevelConfig asset
    [SerializeField] private bool enablePersistence = true; // toggle saving/loading

    [Header("Runtime (read-only)")] [SerializeField, Min(0)]
    private double totalKm;

    [SerializeField, Min(0)] private int money;
    [SerializeField, Min(0)] private int collisions;
    [SerializeField, Min(1)] private int level = 1;
    [SerializeField, Min(0)] private double totalPlaySeconds = 0;
    
    [SerializeField, Tooltip("Debug: last resolved env tier index that was applied.")]
    private int lastAppliedEnvTierIndex = -1;

    // ===== UnityEvents for Inspector wiring =====
    [Serializable]
    public class LevelChangedEvent : UnityEvent<int, int>
    {
    } // (oldLevel, newLevel)

    [Serializable]
    public class KmChangedEvent : UnityEvent<float>
    {
    } // totalKm as float for Inspector

    [Serializable]
    public class MoneyChangedEvent : UnityEvent<int>
    {
    }

    [Serializable]
    public class CollisionsChangedEvent : UnityEvent<int>
    {
    }
    
    [Serializable]
    public class EnvSettingsEvent : UnityEvent<EnvSettings> {}

    [Header("Events")] public LevelChangedEvent OnLevelChanged;
    public KmChangedEvent OnKmChanged;
    public MoneyChangedEvent OnMoneyChanged;
    public CollisionsChangedEvent OnCollisionsChanged;
    
    

    // ===== Public getters =====
    public double TotalKm => totalKm;
    public int Money => money;
    public int Collisions => collisions;
    public int Level => level;
    public LevelConfig Config => levelConfig;
    public double TotalPlaySeconds => totalPlaySeconds;

    private void Awake()
    {
        if (enablePersistence) Load();
        RecalculateLevel(forceEvent: false); // sync level to KM at boot without spamming events
        ApplyCurrentLevelConfiguration();
    }

    // ===================== Public API =====================

    /// Call this from your driving/telemetry system (meters this frame).
    public void AddDistanceMeters(double meters)
    {
        if (meters <= 0) return;

        totalKm += meters / 1000.0;
        OnKmChanged?.Invoke((float)totalKm);

        RecalculateLevel(forceEvent: false);
        if (enablePersistence) Save();
    }

    /// If you already track KM elsewhere, set it directly.
    public void SetTotalKm(double km)
    {
        km = Math.Max(0.0, km);
        if (Math.Abs(km - totalKm) < 1e-6) return;

        totalKm = km;
        OnKmChanged?.Invoke((float)totalKm);

        RecalculateLevel(forceEvent: false);
        if (enablePersistence) Save();
    }

    /// Hook your own money system here. Positive or negative; clamps at 0.
    public void AddMoney(int delta)
    {
        int prev = money;
        long next = (long)money + delta;
        money = (int)Math.Max(0, Math.Min(int.MaxValue, next));
        if (money != prev)
        {
            OnMoneyChanged?.Invoke(money);
            if (enablePersistence) Save();
        }
    }

    public void SetMoney(int value)
    {
        value = Math.Max(0, value);
        if (value == money) return;

        money = value;
        OnMoneyChanged?.Invoke(money);
        if (enablePersistence) Save();
    }

    /// Push total collisions if you track it elsewhere…
    public void SetCollisions(int value)
    {
        value = Math.Max(0, value);
        if (value == collisions) return;

        collisions = value;
        OnCollisionsChanged?.Invoke(collisions);
        if (enablePersistence) Save();
    }

    /// …or just increment when needed.
    public void IncrementCollision()
    {
        collisions++;
        OnCollisionsChanged?.Invoke(collisions);
        if (enablePersistence) Save();
    }

    // ===================== Helpers =====================

    private void RecalculateLevel(bool forceEvent)
    {
        if (levelConfig == null || levelConfig.tiers == null || levelConfig.tiers.Count == 0) return;

        int prevLevel = level;

        var tier = levelConfig.GetTierForKm(totalKm);
        if (tier != null)
        {
            level = Math.Max(1, tier.levelNumber <= 0 ? 1 : tier.levelNumber);
        }

        if (forceEvent || level != prevLevel)
        {
            OnLevelChanged?.Invoke(prevLevel, level);
            ApplyCurrentLevelConfiguration();
        }
    }

    
    
    
    // ===================== Save / Load =====================

    [Serializable]
    private class SaveData
    {
        public double totalKm;
        public int    money;
        public int    collisions;
        public int    level;
        public double totalPlaySeconds; // <-- NEW
    }

    private void Save()
    {
        try
        {
            var data = new SaveData
            {
                totalKm          = this.totalKm,
                money            = this.money,
                collisions       = this.collisions,
                level            = this.level,
                totalPlaySeconds = this.totalPlaySeconds // <-- NEW
            };
            var json = JsonUtility.ToJson(data);
            System.IO.File.WriteAllText(ProgressionSavePath.FilePath, json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ProgressionSystem] Save failed: {e.Message}");
        }
    }

    private void Load()
    {
        try
        {
            string path = ProgressionSavePath.FilePath;
            if (!System.IO.File.Exists(path)) return;

            var json = System.IO.File.ReadAllText(path);
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data != null)
            {
                totalKm          = Math.Max(0.0, data.totalKm);
                money            = Math.Max(0,    data.money);
                collisions       = Math.Max(0,    data.collisions);
                level            = Math.Max(1,    data.level);
                totalPlaySeconds = Math.Max(0.0,  data.totalPlaySeconds); // <-- NEW
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ProgressionSystem] Load failed: {e.Message}");
        }
    }
    
    public void ApplyCurrentLevelConfiguration()
    {
        if (levelConfig == null || levelConfig.tiers == null || levelConfig.tiers.Count == 0) return;

        if (levelConfig.ResolveEnvSettingsForKm(totalKm, out EnvSettings settings, out int resolvedIdx))
        {
            // Avoid re-applying if resolved tier index hasn't changed
            if (resolvedIdx == lastAppliedEnvTierIndex) return;

            lastAppliedEnvTierIndex = resolvedIdx;

            // Notify the scene so it can apply lighting, fog, traffic, etc.
            // OnEnvSettingsApplied?.Invoke(settings);
            LevelEnvConfigurationHandler.Instance.HanldConfig(settings);
        }
        else
        {
            // No settings anywhere below this tier; reset our guard
            lastAppliedEnvTierIndex = -1;
        }
    }
    
    // Optional: if you want to accumulate externally (e.g., from a separate tracker)
    public void AddPlaytime(double seconds)
    {
        if (seconds <= 0) return;
        totalPlaySeconds += seconds;
    }
    
    private void OnApplicationPause(bool pause) { if (pause) Save(); }
    private void OnApplicationQuit()            { Save(); }

#if UNITY_EDITOR
    // Quick test helpers from the component's context menu
    [ContextMenu("Test/Add 1 KM")]
    private void TestAdd1Km() => AddDistanceMeters(1000.0);

    [ContextMenu("Test/Level → Next Tier")]
    private void TestLevelNext()
    {
        if (levelConfig != null && levelConfig.TryGetNextTier(totalKm, out var next))
            SetTotalKm(next.minKm + 0.01);
    }
#endif
}