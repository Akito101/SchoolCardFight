using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlayerProgress
{
    private const string CoinsKey = "Coins";
    private const string ClearedKey = "LevelsCleared";
    private const string SelectedKey = "SelectedLevel";
    private const string FirstBattleKey = "FirstBattleDone";

    public const int TotalLevels = 4;

    public static int Coins => PlayerPrefs.GetInt(CoinsKey, 0);

    public static void AddCoins(int amount)
    {
        PlayerPrefs.SetInt(CoinsKey, Coins + amount);
        PlayerPrefs.Save();
    }

    public static bool SpendCoins(int amount)
    {
        if (Coins < amount) return false;
        PlayerPrefs.SetInt(CoinsKey, Coins - amount);
        PlayerPrefs.Save();
        return true;
    }

    // How many levels the player has beaten (level 1 is the tutorial)
    public static int LevelsCleared => PlayerPrefs.GetInt(ClearedKey, 0);

    // Which level the player picked on the map (the battle scene reads this)
    public static int SelectedLevel
    {
        get => PlayerPrefs.GetInt(SelectedKey, 0);
        set { PlayerPrefs.SetInt(SelectedKey, value); PlayerPrefs.Save(); }
    }

    // The battle scene calls this when a fight ends
    public static void OnBattleFinished(bool won)
    {
        PlayerPrefs.SetInt(FirstBattleKey, 1); // unlocks the Map, win or lose

        if (won && SelectedLevel == LevelsCleared && LevelsCleared < TotalLevels)
        {
            PlayerPrefs.SetInt(ClearedKey, LevelsCleared + 1);
        }

        PlayerPrefs.Save();
        SceneManager.LoadScene("Map");
    }
}