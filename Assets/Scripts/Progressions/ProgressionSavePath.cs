using System.IO;
using UnityEngine;

public static class ProgressionSavePath
{
    private const string FolderName = "Progression";
    private const string FileName   = "player_progress.json";

    public static string DirectoryPath
    {
        get
        {
            string dir = Path.Combine(Application.persistentDataPath, FolderName);
            EnsureDirectory(dir);
            return dir;
        }
    }

    public static string FilePath => Path.Combine(DirectoryPath, FileName);

    private static void EnsureDirectory(string dir)
    {
        try
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ProgressionSavePath] Could not ensure directory: {dir}\n{e.Message}");
        }
    }
}