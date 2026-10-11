using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CardCount
{
    public string cardName;
    public int count;
}

[System.Serializable]
public class CollectionSave
{
    public List<CardCount> cards = new List<CardCount>();
}

public static class PlayerCollection
{
    private const string Key = "Collection";
    private static CollectionSave data;

    private static void EnsureLoaded()
    {
        if (data != null) return;
        string json = PlayerPrefs.GetString(Key, "");
        data = string.IsNullOrEmpty(json) ? new CollectionSave() : JsonUtility.FromJson<CollectionSave>(json);
    }

    public static void AddCard(string cardName, int amount = 1)
    {
        EnsureLoaded();
        CardCount entry = data.cards.Find(c => c.cardName == cardName);
        if (entry == null)
        {
            entry = new CardCount { cardName = cardName, count = 0 };
            data.cards.Add(entry);
        }
        entry.count += amount;
        Save();
    }

    public static int GetCount(string cardName)
    {
        EnsureLoaded();
        CardCount entry = data.cards.Find(c => c.cardName == cardName);
        return entry == null ? 0 : entry.count;
    }

    public static List<CardCount> GetAll()
    {
        EnsureLoaded();
        return data.cards;
    }

    public static void Clear()
    {
        data = new CollectionSave();
        PlayerPrefs.DeleteKey(Key);
    }

    private static void Save()
    {
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }
}