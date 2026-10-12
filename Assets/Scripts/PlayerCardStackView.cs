using UnityEngine;
using System.Collections.Generic;

public class PlayerCardStackView : MonoBehaviour
{
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private GameObject cardBackPrefab;


    private RectTransform playerDeckTransform;
    private Vector2 stackOffset = new Vector2(12f, 0f);
    private int maxVisibleLayers = 4;
    private List<GameObject> playerCardBackDeck = new List<GameObject>();

    void Awake()
    {
        playerDeckTransform = GetComponent<RectTransform>();
    }

    public CardView SpawnCard()
    {
        CardView newCard = Instantiate(cardPrefab, transform.parent);
        newCard.SetOriginalPosition(playerDeckTransform.anchoredPosition);
        return newCard;
    }

    public void BuildDeck(int cardCount)
    {
        ClearStack();
        for (int i = 0; i < cardCount; i++)
        {
            GameObject card = Instantiate(cardBackPrefab, playerDeckTransform);
            playerCardBackDeck.Add(card);
        }

        RepositionStack();
    }

    private void ClearStack()
    {
        foreach (GameObject card in playerCardBackDeck)
        {
            Destroy(card);
        }
        playerCardBackDeck.Clear();
    }

    public void RemoveTopCard()
    {
        if (playerCardBackDeck.Count == 0) return;

        int topIndex = playerCardBackDeck.Count - 1;
        GameObject topCard = playerCardBackDeck[topIndex];
        Destroy(topCard);
        playerCardBackDeck.RemoveAt(topIndex);

        RepositionStack();
    }

    private void RepositionStack()
    {
        int visibleLayerCount = Mathf.Max(1, maxVisibleLayers);

        for (int i = 0; i < playerCardBackDeck.Count; i++)
        {
            RectTransform rt = playerCardBackDeck[i].GetComponent<RectTransform>();
            int layer = Mathf.Min(playerCardBackDeck.Count - 1 - i, visibleLayerCount - 1);
            rt.anchoredPosition = stackOffset * layer;
            rt.SetSiblingIndex(i);
        }
    }
}