using UnityEngine;
using System.Collections.Generic;

public class PlayerDeckView : MonoBehaviour
{
    [SerializeField] private CardView cardPrefab;

    private List<CardView> playerDeckCards = new List<CardView>();
    private RectTransform playerDeckTransform;
    private Vector2 stackOffset = new Vector2(12f, 0f);
    private int maxVisibleLayers = 4;

    public bool HasCardsRemaining
    {
        get { return playerDeckCards.Count > 0; }
    }

    void Awake()
    {
        playerDeckTransform = GetComponent<RectTransform>();
    }

    public void BuildDeck(List<CardData> baseCards, CardData artifactCard)
    {
        ClearDeck();

        // Front Deck
        List<CardData> tempDataSet = new List<CardData>(baseCards);
        ShuffleDeck(tempDataSet);
        InsertArtifactEarly(tempDataSet, artifactCard);

        // Back Deck
        foreach (CardData data in tempDataSet)
        {
            CardView newCard = Instantiate(cardPrefab, playerDeckTransform);
            newCard.Setup(data);
            playerDeckCards.Add(newCard);
        }
        RepositionStack();
    }

    private void ShuffleDeck(List<CardData> list)
    {
        // Fisher-Yates shuffle - a standard, reliable way to randomize a list
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }

    private void InsertArtifactEarly(List<CardData> list, CardData artifactCard)
    {
        if (artifactCard == null) return;
        int maxPosition = Mathf.Max(1, list.Count / 2);
        int insertPosition = Random.Range(0, maxPosition);
        list.Insert(insertPosition, artifactCard);
    }

    // Call this when the player's turn starts - removes and returns the top card
    public CardData DrawCard()
    {
        if (playerDeckCards.Count == 0)
        {
            Debug.Log("frontDeck is empty - this is your lose condition trigger point");
            return null;
        }

        int topIndex = playerDeckCards.Count - 1;
        CardView topCardView = playerDeckCards[topIndex];
        CardData drawnCard = topCardView.GetCurrentCardData();

        playerDeckCards.RemoveAt(topIndex);
        Destroy(topCardView.gameObject);

        RepositionStack();

        return drawnCard;
    }

    public CardView SpawnCard()
    {
        CardView newCard = Instantiate(cardPrefab, transform.parent);
        newCard.SetOriginalPosition(playerDeckTransform.anchoredPosition);
        newCard.gameObject.SetActive(false);
        return newCard;
    }

    private void ClearDeck()
    {
        foreach (CardView card in playerDeckCards)
        {
            if (card != null) Destroy(card.gameObject);
        }
        playerDeckCards.Clear();
    }

    private void RepositionStack()
    {
        int visibleLayerCount = Mathf.Max(1, maxVisibleLayers);

        for (int i = 0; i < playerDeckCards.Count; i++)
        {
            RectTransform rt = playerDeckCards[i].GetComponent<RectTransform>();
            int layer = Mathf.Min(playerDeckCards.Count - 1 - i, visibleLayerCount - 1);
            rt.anchoredPosition = stackOffset * layer;
            rt.SetSiblingIndex(i);
        }
    }
}