using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIProgressReceiver : MonoBehaviour
{
    [Header("Level Text")]
    public TMP_Text levelNumberText;   // "7"
    public TMP_Text levelNameText;     // "Pro"

    [Header("Stats Text")]
    public TMP_Text playtimeText;      // "12:34:56"
    public TMP_Text collisionsText;    // "15"
    public TMP_Text moneyText;         // "$12,345"
    public TMP_Text kmText;            // "123.4 km"

    [Header("Images")]
    public Image badgeImage;           // with fallback
    public Image avatarImage;          // with fallback

    [Header("References")]
    public LevelConfig levelConfig;    // assign your LevelConfig asset

    [Header("Formatting")]
    [Tooltip("KM shown with this many fractional digits.")]
    [Range(0, 3)] public int kmDecimals = 1;
    [Tooltip("Prefix for money.")]
    public string moneyPrefix = "$";

    [Header("Behavior")]
    [Tooltip("If true, refresh once every time this GO becomes active.")]
    public bool refreshOnEnable = true;

    private void OnEnable()
    {
        if (refreshOnEnable)
            RefreshNow();
    }
    

    /// Call this after you open/show the panel to pull fresh data.
    public void RefreshNow()
    {
        if (ProgressionSystem.Instance == null || levelConfig == null) return;

        // --- Pull raw data from the system ---
        double totalKm        = ProgressionSystem.Instance.TotalKm;
        int    money          = ProgressionSystem.Instance.Money;
        int    collisions     = ProgressionSystem.Instance.Collisions;
        double totalPlaySec   = ProgressionSystem.Instance.TotalPlaySeconds;

        // --- Resolve visuals with fallbacks (badge + avatar + level strings) ---
        levelConfig.ResolveDisplayForKm(
            totalKm,
            out int levelNumber,
            out string levelName,
            out Sprite resolvedBadge,
            out Sprite resolvedAvatar
        );

        // --- Fill Texts ---
        if (levelNumberText) levelNumberText.text = levelNumber.ToString();
        if (levelNameText)   levelNameText.text   = levelName;

        if (kmText)          kmText.text          = $"{totalKm.ToString($"F{kmDecimals}")} km";
        if (moneyText)       moneyText.text       = $"{moneyPrefix}{money:N0}";
        if (collisionsText)  collisionsText.text  = collisions.ToString();
        if (playtimeText)    playtimeText.text    = FormatHMS(totalPlaySec);

        // --- Fill Images (hide if still null after fallback) ---
        if (badgeImage)  { badgeImage.sprite = resolvedBadge;  badgeImage.gameObject.SetActive(badgeImage.sprite  != null); }
        if (avatarImage) { avatarImage.sprite = resolvedAvatar; avatarImage.gameObject.SetActive(avatarImage.sprite != null); }
    }

    private static string FormatHMS(double seconds)
    {
        if (seconds < 0) seconds = 0;
        var t = System.TimeSpan.FromSeconds(seconds);
        // If you expect sessions longer than 24h and want days shown, use: $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}"
        return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
    }
}
