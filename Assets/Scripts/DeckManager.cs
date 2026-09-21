using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    [Header("Deck Setup")]
    public List<CardData> deck = new List<CardData>();

    // Call this to build and shuffle a fresh deck before a battle starts
    public void BuildDeck(List<CardData> baseCards, CardData artifactCard)
    {
        deck = new List<CardData>(baseCards);
        ShuffleDeck();

        if (artifactCard != null)
        {
            InsertArtifactEarly(artifactCard);
        }
    }

    private void ShuffleDeck()
    {
        // Fisher-Yates shuffle - a standard, reliable way to randomize a list
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (deck[i], deck[randomIndex]) = (deck[randomIndex], deck[i]);
        }
    }

    private void InsertArtifactEarly(CardData artifactCard)
    {
        // Guarantees the Artifact lands somewhere in the first half of the deck,
        // so players never draw it too late to use it
        int maxPosition = Mathf.Max(1, deck.Count / 2);
        int insertPosition = Random.Range(0, maxPosition);
        deck.Insert(insertPosition, artifactCard);
    }

    // Call this when the player's turn starts - removes and returns the top card
    public CardData DrawCard()
    {
        if (deck.Count == 0)
        {
            Debug.Log("Deck is empty - this is your lose condition trigger point");
            return null;
        }

        CardData drawnCard = deck[0];
        deck.RemoveAt(0);
        return drawnCard;
    }

    public int CardsRemaining()
    {
        return deck.Count;
    }
}