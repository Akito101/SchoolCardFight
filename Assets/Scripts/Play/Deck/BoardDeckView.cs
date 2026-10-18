using UnityEngine;
using System.Collections.Generic;
using System;

public class BoardDeckView : MonoBehaviour
{
    [SerializeField] private List<CardData> boardCardDeck = new List<CardData>();
    private Vector2 positionJitter = new Vector2(18f, 12f);
    private float maximumRotation = 15f;
    private RectTransform boardTransform;

    private void Awake()
    {
        boardTransform = GetComponent<RectTransform>();
    }

    public void ClearBoard()
    {
        boardCardDeck.Clear();
    }

    public void AddCardToBoardDeck(CardData cardData, CardView cardView, Action onAnimationComplete)
    {
        if (cardData == null || cardView == null)
        {
            Debug.LogWarning("Cannot add a null card or card view to the board stack");
            return;
        }

        boardCardDeck.Add(cardData);
        cardView.transform.SetAsLastSibling();

        // 1. Calculate random card position on the board
        Vector2 boardPosition = boardTransform.anchoredPosition
            + new Vector2(
                UnityEngine.Random.Range(-positionJitter.x, positionJitter.x),
                UnityEngine.Random.Range(-positionJitter.y, positionJitter.y));

        // 2. Calculate perspective scale based on vertical position (Y-axis)
        float boardHeightArea = 120f;
        float normalizedY = Mathf.InverseLerp(
            boardTransform.anchoredPosition.y - boardHeightArea,
            boardTransform.anchoredPosition.y + boardHeightArea,
            boardPosition.y
        );

        float perspectiveScale = Mathf.Lerp(0.65f, 1.1f, Mathf.Clamp01(normalizedY));
        float depthTilt = Mathf.Lerp(-6f, 6f, Mathf.Clamp01(normalizedY));

        // 3. Random rotation mixed with a slight perspective tilt
        float cardRotation = UnityEngine.Random.Range(-maximumRotation, maximumRotation);

        cardView.DrawCardAnimation(boardPosition, cardRotation, perspectiveScale, onAnimationComplete);
    }
}