using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    [Header("PlayerCardFrontDeck Setup")]
    [SerializeField] private List<CardData> playerCardFrontDeck = new List<CardData>();

    // Call this to build and shuffle a fresh playerCardFrontDeck before a battle starts
    public void BuildDeck(List<CardData> baseCards, CardData artifactCard)
    {
        playerCardFrontDeck = new List<CardData>(baseCards);
        ShuffleDeck();

        if (artifactCard != null)
        {
            InsertArtifactEarly(artifactCard);
        }
    }

    private void ShuffleDeck()
    {
        // Fisher-Yates shuffle - a standard, reliable way to randomize a list
        for (int i = playerCardFrontDeck.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (playerCardFrontDeck[i], playerCardFrontDeck[randomIndex]) = (playerCardFrontDeck[randomIndex], playerCardFrontDeck[i]);
        }
    }

    private void InsertArtifactEarly(CardData artifactCard)
    {
        // Guarantees the Artifact lands somewhere in the first half of the playerCardFrontDeck,
        // so players never draw it too late to use it
        int maxPosition = Mathf.Max(1, playerCardFrontDeck.Count / 2);
        int insertPosition = Random.Range(0, maxPosition);
        playerCardFrontDeck.Insert(insertPosition, artifactCard);
    }

    public int CardsRemaining()
    {
        return playerCardFrontDeck.Count;
    }

    // Call this when the player's turn starts - removes and returns the top card
    public CardData DrawCard()
    {
        if (playerCardFrontDeck.Count == 0)
        {
            Debug.Log("playerCardFrontDeck is empty - this is your lose condition trigger point");
            return null;
        }

        int topIndex = playerCardFrontDeck.Count - 1;
        CardData drawnCard = playerCardFrontDeck[topIndex];
        playerCardFrontDeck.RemoveAt(topIndex);

        return drawnCard;
    }
}