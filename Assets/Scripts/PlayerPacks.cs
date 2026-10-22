using UnityEngine;

public static class PlayerPacks
{
    private const string Key = "UnopenedPacks";

    public static int Count => PlayerPrefs.GetInt(Key, 0);

    public static void Add(int amount = 1)
    {
        PlayerPrefs.SetInt(Key, Count + amount);
        PlayerPrefs.Save();
    }

    // Uses up one pack. Returns false if there are none.
    public static bool Use()
    {
        if (Count <= 0) return false;
        PlayerPrefs.SetInt(Key, Count - 1);
        PlayerPrefs.Save();
        return true;
    }
}