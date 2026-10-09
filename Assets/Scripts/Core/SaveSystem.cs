using UnityEngine;

public static class SaveSystem
{
    private const string MaxLevelKey = "MaxLevelUnlocked";
    private const string HighScoreKeyPrefix = "HighScore_Level_";

    public static void SaveLevelProgress(int levelIndex, int score)
    {
        int currentMaxLevel = PlayerPrefs.GetInt(MaxLevelKey, 1);
        if (levelIndex + 1 > currentMaxLevel)
        {
            PlayerPrefs.SetInt(MaxLevelKey, levelIndex + 1);
        }

        string scoreKey = HighScoreKeyPrefix + levelIndex;
        int currentHighScore = PlayerPrefs.GetInt(scoreKey, 0);
        if (score > currentHighScore)
        {
            PlayerPrefs.SetInt(scoreKey, score);
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
}
