using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image healthBar;

    public void SetHealthBar(int health)
    {
        if (healthBar == null)
        {
            Debug.LogWarning("HealthBar Image reference is missing.");
            return;
        }

        // Clamp health to 0..100
        int clamped = Mathf.Clamp(health, 0, 100);
        float t = clamped / 100f; // 0..1

        // Scale X relative to health (ensure your bar's pivot is set to the left)
        RectTransform rt = healthBar.rectTransform;
        Vector3 scale = rt.localScale;
        scale.x = t;          // 0 at 0 health, 1 at 100+
        rt.localScale = scale;

        // Gradient color: 0 = red, 100 = green
        healthBar.color = Color.Lerp(Color.red, Color.green, t);
    }
}