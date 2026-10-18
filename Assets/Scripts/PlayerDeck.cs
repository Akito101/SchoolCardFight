using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BenchSave
{
    public List<CardCount> benched = new List<CardCount>();
}

public static class PlayerDeck
{
    private const string Key = "DeckBench";
    private static BenchSave data;

    private static void EnsureLoaded()
    {
        if (data != null) return;
        string json = PlayerPrefs.GetString(Key, "");
        data = string.IsNullOrEmpty(json) ? new BenchSave() : JsonUtility.FromJson<BenchSave>(json);
        if (data.benched == null) data.benched = new List<CardCount>();
    }

    // Copies of this card kept out of the deck (never more than you own)
    public static int GetBenched(string cardName)
    {
        EnsureLoaded();
        CardCount entry = data.benched.Find(c => c.cardName == cardName);
        int saved = entry == null ? 0 : entry.count;
        return Mathf.Min(saved, PlayerCollection.GetCount(cardName));
    }

    // Copies of this card that are in the deck
    public static int GetInDeck(string cardName)
    {
        return PlayerCollection.GetCount(cardName) - GetBenched(cardName);
    }

    public static int TotalInDeck()
    {
        int total = 0;
        foreach (CardCount c in PlayerCollection.GetAll()) total += GetInDeck(c.cardName);
        return total;
    }

    public static bool MoveToBench(string cardName)
    {
        if (GetInDeck(cardName) <= 0) return false;
        SetBenched(cardName, GetBenched(cardName) + 1);
        return true;
    }

    public static bool MoveToDeck(string cardName)
    {
        int benched = GetBenched(cardName);
        if (benched <= 0) return false;
        SetBenched(cardName, benched - 1);
        return true;
    }

    public static void AddAllToDeck()
    {
        EnsureLoaded();
        data.benched.Clear();
        Save();
    }

    // The battle scene uses this to get the player's deck as a list of cards
    public static List<CardData> ToCardList(CardDatabase database)
    {
        List<CardData> list = new List<CardData>();
        foreach (CardCount c in PlayerCollection.GetAll())
        {
            CardData card = database.GetByName(c.cardName);
            if (card == null) continue;

            int copies = GetInDeck(c.cardName);
            for (int i = 0; i < copies; i++) list.Add(card);
        }
        return list;
    }

    public static void Clear()
    {
        data = new BenchSave();
        PlayerPrefs.DeleteKey(Key);
    }

    private static void SetBenched(string cardName, int amount)
    {
        EnsureLoaded();
        CardCount entry = data.benched.Find(c => c.cardName == cardName);

        if (entry == null)
        {
            if (amount <= 0) return;
            entry = new CardCount { cardName = cardName, count = amount };
            data.benched.Add(entry);
        }
        else
        {
            entry.count = amount;
            if (entry.count <= 0) data.benched.Remove(entry);
        }
        Save();
    }

    private static void Save()
    {
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }
}