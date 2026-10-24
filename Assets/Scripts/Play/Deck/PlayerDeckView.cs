using UnityEngine;
using System.Collections.Generic;

public class PlayerDeckView : MonoBehaviour
{
    [SerializeField] private CardView cardPrefab;

    private readonly List<CardData> playerDeckData = new List<CardData>();
    private readonly List<CardView> playerDeckCards = new List<CardView>();

    private RectTransform playerDeckTransform;
    private Vector2 stackOffset = new Vector2(8f, 0f);
    private int maxVisibleLayers = 4;

    public int GetCardCount()
    {
        return playerDeckData.Count;
    }

    void Awake()
    {
        playerDeckTransform = GetComponent<RectTransform>();
    }

    public void BuildDeck(List<CardData> initialDeck, CardData artifactCard)
    {
        ClearDeck();


        playerDeckData.AddRange(initialDeck);
        ShuffleDeck(playerDeckData);
        InsertArtifactEarly(playerDeckData, artifactCard);

        RebuildDeckViews();
    }

    public void AddCardsAndReshuffle(List<CardData> winedDeck)
    {
        if (winedDeck == null || winedDeck.Count == 0) return;

        playerDeckData.AddRange(winedDeck);
        ShuffleDeck(playerDeckData);
        RebuildDeckViews();
    }

    private void RebuildDeckViews()
    {
        foreach (CardData data in playerDeckData)
        {
            CardView newCard = Instantiate(cardPrefab, playerDeckTransform);
            newCard.ShowFaceDown();
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

    public CardData DrawCard()
    {
        if (playerDeckData.Count == 0) return null;

        int topIndex = playerDeckData.Count - 1;

        CardData drawnCard = playerDeckData[topIndex];
        playerDeckData.RemoveAt(topIndex);

        CardView topCardView = playerDeckCards[topIndex];
        playerDeckCards.RemoveAt(topIndex);

        if (topCardView != null)
        {
            Destroy(topCardView.gameObject);
        }

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

    public void PrepareDrawnCard(CardView currentCardView, CardData currentCardData)
    {
        if (currentCardView == null || currentCardData == null)
        {
            Debug.LogWarning("Cannot prepare a null card view or card data.");
            return;
        }

        currentCardView.SetCardFront(currentCardData.cardFrontSprite);
    }

    private void ClearDeck()
    {
        foreach (CardView card in playerDeckCards)
        {
            if (card != null) Destroy(card.gameObject);
        }
        playerDeckCards.Clear();
        playerDeckData.Clear();
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