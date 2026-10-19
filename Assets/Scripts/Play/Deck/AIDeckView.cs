using UnityEngine;
using System.Collections.Generic;

public class AIDeckView : MonoBehaviour
{
    [SerializeField] private CardView cardPrefab;

    private readonly List<CardData> aiDeckData = new List<CardData>();
    private readonly List<CardView> aiDeckView = new List<CardView>();

    private RectTransform aiDeckTransform;
    private Vector2 stackOffset = new Vector2(-12f, 0f);
    private int maxVisibleLayers = 4;

    public bool HasCardsRemaining
    {
        get { return aiDeckData.Count > 4; }
    }

    public int GetCardCount()
    {
        return aiDeckData.Count;
    }

    void Awake()
    {
        aiDeckTransform = GetComponent<RectTransform>();
    }

    public void BuildDeck(List<CardData> baseCards, CardData artifactCard)
    {
        ClearDeck();

        // Front Deck
        aiDeckData.AddRange(baseCards);
        ShuffleDeck(aiDeckData);
        InsertArtifactEarly(aiDeckData, artifactCard);

        // Back Deck
        foreach (CardData data in aiDeckData)
        {
            CardView newCard = Instantiate(cardPrefab, aiDeckTransform);
            newCard.ShowFaceDown();
            aiDeckView.Add(newCard);
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
        if (aiDeckData.Count == 0)
        {
            Debug.Log("frontDeck is empty - this is your lose condition trigger point");
            return null;
        }

        int topIndex = aiDeckData.Count - 1;

        CardData drawnCard = aiDeckData[topIndex];
        aiDeckData.RemoveAt(topIndex);

        CardView topCardView = aiDeckView[topIndex];
        aiDeckView.RemoveAt(topIndex);

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
        newCard.SetOriginalPosition(aiDeckTransform.anchoredPosition);
        newCard.gameObject.SetActive(false);
        return newCard;
    }

    public void PrepareDrawnCard(CardView cardView, CardData cardData)
    {
        if (cardView == null || cardData == null)
        {
            Debug.LogWarning("Cannot prepare a null card view or card data.");
            return;
        }

        cardView.SetCardFront(cardData.cardFrontSprite);
    }

    private void ClearDeck()
    {
        foreach (CardView card in aiDeckView)
        {
            if (card != null) Destroy(card.gameObject);
        }
        aiDeckView.Clear();
    }

    private void RepositionStack()
    {
        int visibleLayerCount = Mathf.Max(1, maxVisibleLayers);

        for (int i = 0; i < aiDeckView.Count; i++)
        {
            RectTransform rt = aiDeckView[i].GetComponent<RectTransform>();
            int layer = Mathf.Min(aiDeckView.Count - 1 - i, visibleLayerCount - 1);
            rt.anchoredPosition = stackOffset * layer;
            rt.SetSiblingIndex(i);
        }
    }
}