using UnityEngine;

public class PlaytimeTracker : MonoBehaviour
{
    [Tooltip("If false, stops counting when the app window loses focus.")]
    public bool countWhenUnfocused = false;

    void Update()
    {
        if (ProgressionSystem.Instance == null) return;
        if (!countWhenUnfocused && !Application.isFocused) return;

        ProgressionSystem.Instance.AddPlaytime(Time.unscaledDeltaTime);
    }

    void OnApplicationPause(bool pause)
    {
        if (pause && ProgressionSystem.Instance != null)
            // persist frequently on mobile
            typeof(ProgressionSystem)
                .GetMethod("Save", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(ProgressionSystem.Instance, null);
    }

    void OnApplicationQuit()
    {
        if (ProgressionSystem.Instance != null)
            typeof(ProgressionSystem)
                .GetMethod("Save", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(ProgressionSystem.Instance, null);
    }
}