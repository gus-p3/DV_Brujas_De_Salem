using UnityEngine;

public static class SaveSystem
{
    private const string MaxLevelKey = "MaxLevelUnlocked";
    private const string HighScoreKeyPrefix = "HighScore_Level_";

    public const string UnlockedLevelKey = "UnlockedLevel";
    public const string HighScoreLevel1Key = "HighScore_Level1";

    public static void SaveLevelProgress(int levelIndex, int score)
    {
        int currentMaxLevel = PlayerPrefs.GetInt(MaxLevelKey, 1);
        if (levelIndex + 1 > currentMaxLevel)
        {
            PlayerPrefs.SetInt(MaxLevelKey, levelIndex + 1);
        }

        // Compatibilidad con clave alternativa
        if (levelIndex + 1 > PlayerPrefs.GetInt(UnlockedLevelKey, 1))
        {
            PlayerPrefs.SetInt(UnlockedLevelKey, levelIndex + 1);
        }

        string scoreKey = HighScoreKeyPrefix + levelIndex;
        int currentHighScore = PlayerPrefs.GetInt(scoreKey, 0);
        if (score > currentHighScore)
        {
            PlayerPrefs.SetInt(scoreKey, score);
        }

        if (levelIndex == 1)
        {
            int high1 = PlayerPrefs.GetInt(HighScoreLevel1Key, 0);
            if (score > high1)
            {
                PlayerPrefs.SetInt(HighScoreLevel1Key, score);
            }
        }

        PlayerPrefs.Save();
    }

    public static int GetMaxLevelUnlocked()
    {
        return PlayerPrefs.GetInt(MaxLevelKey, 1);
    }

    public static bool HasProgress()
    {
        return PlayerPrefs.HasKey(MaxLevelKey);
    }

    public static int GetHighScore(int levelIndex)
    {
        return PlayerPrefs.GetInt(HighScoreKeyPrefix + levelIndex, 0);
    }
}
