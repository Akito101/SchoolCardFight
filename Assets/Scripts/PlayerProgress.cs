using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class PlayerProgress
{
    private const string CoinsKey = "Coins";
    private const string FirstBattleKey = "FirstBattleDone";
    private const string ClearedLevelsKey = "ClearedLevels";
    private const string LevelSelectedKey = "LevelSelected";
    private const string OwnedCardsKey = "OwnedCardNames";
    private const string OwnedArtifactsKey = "OwnedArtifactsTypes";
    private const string CollectionBookKey = "CardCollection";
    private const string BattleDecksKey = "BattleDecksSaved";

    public const int TotalLevels = 4;
    public static int Coins => PlayerPrefs.GetInt(CoinsKey, 0);

    // Coin Management
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

    // Level Management
    public static int ClearedLevels => PlayerPrefs.GetInt(ClearedLevelsKey, 0);

    public static int LevelSelected
    {
        get => PlayerPrefs.GetInt(LevelSelectedKey, 0);
        set { PlayerPrefs.SetInt(LevelSelectedKey, value); PlayerPrefs.Save(); }
    }

    // Battle Outcome Management
    public static void OnBattleFinished(bool won)
    {
        PlayerPrefs.SetInt(FirstBattleKey, 1); // unlocks the Map, win or lose

        if (won && LevelSelected == ClearedLevels && ClearedLevels < TotalLevels)
        {
            PlayerPrefs.SetInt(ClearedLevelsKey, ClearedLevels + 1);
        }

        PlayerPrefs.Save();
        SceneManager.LoadScene("Map");
    }

    // Cards Management
    public static List<string> GetOwnedBattleCards()
    {
        return LoadStringList(OwnedCardsKey);
    }

    public static void SaveOwnedBattleCards(List<string> cardNames)
    {
        SaveStringList(OwnedCardsKey, cardNames);
    }

    // Artifact Management
    public static List<string> GetOwnedArtifacts()
    {
        return LoadStringList(OwnedArtifactsKey);
    }

    public static void SaveOwnedArtifacts(List<string> artifactTypes)
    {
        SaveStringList(OwnedArtifactsKey, artifactTypes);
    }

    // Collection Management
    public static List<string> GetCollectionBook()
    {
        return LoadStringList(CollectionBookKey);
    }

    public static void SaveCollectionBook(List<string> uniqueCardNames)
    {
        SaveStringList(CollectionBookKey, uniqueCardNames);
    }

    // Saved Deck Management
    public static List<string> GetBattleDeckCards()
    {
        return LoadStringList(BattleDecksKey);
    }

    public static void SaveBattleDeckCards(List<string> deckCardNames)
    {
        SaveStringList(BattleDecksKey, deckCardNames);
    }

    // Helper methods for saving and loading lists of strings using JSON serialization
    [System.Serializable]
    private class SerializationWrapper<T>
    {
        public List<T> items;
        public SerializationWrapper(List<T> items) { this.items = items; }
    }

    private static List<string> LoadStringList(string key)
    {
        if (!PlayerPrefs.HasKey(key)) return new List<string>();
        string json = PlayerPrefs.GetString(key);
        var wrapper = JsonUtility.FromJson<SerializationWrapper<string>>(json);
        return wrapper != null ? wrapper.items : new List<string>();
    }

    private static void SaveStringList(string key, List<string> list)
    {
        string json = JsonUtility.ToJson(new SerializationWrapper<string>(list));
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
    }
}