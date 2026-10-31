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
    public List<CardCount> cards = new List<CardCount>();      // what you own right now
    public List<string> discovered = new List<string>();       // every card you ever got
}

public static class PlayerCollection
{
    private static CollectionSave data;

    private static void EnsureLoaded()
    {
        if (data != null) return;

        data = new CollectionSave();

        // 1. Load total owned card counts via PlayerProgress (OwnedCardsKey)
        List<CardCount> savedOwnedCards = PlayerProgress.GetOwnedBattleCards();
        if (savedOwnedCards != null)
        {
            data.cards = savedOwnedCards;
        }

        // 2. Load discovered collection book via PlayerProgress (CollectionBookKey)
        List<string> savedDiscovered = PlayerProgress.GetCollectionBook();
        if (savedDiscovered != null)
        {
            data.discovered = savedDiscovered;
        }

        if (data.discovered == null) data.discovered = new List<string>();

        // Older saves: anything already owned counts as discovered
        foreach (CardCount c in data.cards)
        {
            if (c.count > 0 && !data.discovered.Contains(c.cardName))
                data.discovered.Add(c.cardName);
        }
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

        if (!data.discovered.Contains(cardName)) data.discovered.Add(cardName);
        Save();
    }

    // Call this when the player loses a card (the book still remembers it)
    public static void RemoveCard(string cardName, int amount = 1)
    {
        EnsureLoaded();

        CardCount entry = data.cards.Find(c => c.cardName == cardName);
        if (entry == null) return;

        entry.count -= amount;
        if (entry.count <= 0) data.cards.Remove(entry);
        Save();

        // the deck must never keep cards the player no longer owns
        PlayerDeck.Prune();
    }

    public static int GetCount(string cardName)
    {
        EnsureLoaded();
        CardCount entry = data.cards.Find(c => c.cardName == cardName);
        return entry == null ? 0 : entry.count;
    }

    public static bool IsDiscovered(string cardName)
    {
        EnsureLoaded();
        return data.discovered.Contains(cardName);
    }

    public static List<CardCount> GetAll()
    {
        EnsureLoaded();
        return data.cards;
    }

    public static void Clear()
    {
        data = new CollectionSave();
        Save();
        PlayerDeck.Prune();
    }

    private static void Save()
    {
        // Delegate all persistence strictly to PlayerProgress database
        PlayerProgress.SaveOwnedBattleCards(data.cards);
        PlayerProgress.SaveCollectionBook(data.discovered);
    }
}