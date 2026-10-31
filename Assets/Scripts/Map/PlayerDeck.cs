using System.Collections.Generic;
using UnityEngine;

public static class PlayerDeck
{
    private static List<string> activeDeckCache = null;

    private static void EnsureLoaded()
    {
        if (activeDeckCache != null) return;

        // Load exclusively from PlayerProgress database
        activeDeckCache = PlayerProgress.GetBattleDeckCards();
        if (activeDeckCache == null) activeDeckCache = new List<string>();

        // Old saves can contain cards the player has since lost: clean them out once on load
        Prune();
    }

    // Removes deck entries for cards the player no longer owns (or owns fewer copies of).
    // The deck can never list more copies of a card than the collection holds.
    public static void Prune()
    {
        if (activeDeckCache == null) return;

        Dictionary<string, int> kept = new Dictionary<string, int>();
        List<string> clean = new List<string>(activeDeckCache.Count);

        foreach (string name in activeDeckCache)
        {
            int owned = PlayerCollection.GetCount(name);
            kept.TryGetValue(name, out int k);

            if (k >= owned) continue;   // not owned (anymore) or too many copies: drop it

            kept[name] = k + 1;
            clean.Add(name);
        }

        if (clean.Count != activeDeckCache.Count)
        {
            activeDeckCache = clean;
            Save();
        }
    }

    // Copies of this card kept out of the deck (calculated by what is NOT in the active deck list)
    public static int GetBenched(string cardName)
    {
        EnsureLoaded();
        int totalOwned = PlayerCollection.GetCount(cardName);
        int inDeckCount = 0;

        foreach (string name in activeDeckCache)
        {
            if (name == cardName) inDeckCount++;
        }

        int benched = totalOwned - inDeckCount;
        return Mathf.Max(0, benched);
    }

    // Copies of this card that are in the deck
    public static int GetInDeck(string cardName)
    {
        EnsureLoaded();
        int count = 0;
        foreach (string name in activeDeckCache)
        {
            if (name == cardName) count++;
        }
        // Never exceed what the player actually owns in collection
        return Mathf.Min(count, PlayerCollection.GetCount(cardName));
    }

    // Real deck size: only cards the player still owns
    public static int TotalInDeck()
    {
        EnsureLoaded();
        Prune();
        return activeDeckCache.Count;
    }

    public static bool MoveToBench(string cardName)
    {
        EnsureLoaded();
        // To bench a card, it must currently be in the deck
        if (GetInDeck(cardName) <= 0) return false;

        // Remove one instance of this card from the active deck cache list
        activeDeckCache.Remove(cardName);
        Save();
        return true;
    }

    public static bool MoveToDeck(string cardName)
    {
        EnsureLoaded();
        // To move to deck, player must own more copies than are currently in the deck
        int owned = PlayerCollection.GetCount(cardName);
        int inDeck = GetInDeck(cardName);
        if (inDeck >= owned) return false;

        // Add an instance to the active deck cache list
        activeDeckCache.Add(cardName);
        Save();
        return true;
    }

    // Automatically populates the deck up to a specified limit if it is currently empty
    public static void EnsureDeckPopulatedUpTo(int limit)
    {
        EnsureLoaded();
        Prune();
        if (activeDeckCache.Count > 0) return; // Already has cards, do nothing

        foreach (CardCount c in PlayerCollection.GetAll())
        {
            int owned = c.count;
            for (int i = 0; i < owned; i++)
            {
                if (activeDeckCache.Count >= limit) break;
                activeDeckCache.Add(c.cardName);
            }
            if (activeDeckCache.Count >= limit) break;
        }
        Save();
    }

    public static void AddAllToDeck()
    {
        EnsureLoaded();
        activeDeckCache.Clear();

        // Populate deck with all available cards from collection up to limit/ownership
        foreach (CardCount c in PlayerCollection.GetAll())
        {
            for (int i = 0; i < c.count; i++)
            {
                activeDeckCache.Add(c.cardName);
            }
        }
        Save();
    }

    // The battle scene uses this to get the player's deck as a list of cards
    public static List<CardData> ToCardList(CardDatabase database)
    {
        EnsureLoaded();
        Prune();
        List<CardData> list = new List<CardData>();
        foreach (string cardName in activeDeckCache)
        {
            CardData card = database.GetByName(cardName);
            if (card != null) list.Add(card);
        }
        return list;
    }

    public static void Clear()
    {
        EnsureLoaded();
        activeDeckCache.Clear();
        Save();
    }

    private static void Save()
    {
        // Delegate all persistence strictly to PlayerProgress database
        PlayerProgress.SaveBattleDeckCards(activeDeckCache);
    }

    public static List<string> GetActiveDeckCardNames()
    {
        EnsureLoaded();
        Prune();
        return new List<string>(activeDeckCache);
    }
}