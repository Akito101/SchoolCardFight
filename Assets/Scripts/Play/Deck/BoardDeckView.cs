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

        Vector2 boardPosition = boardTransform.anchoredPosition
            + new Vector2(
                UnityEngine.Random.Range(-positionJitter.x, positionJitter.x),
                UnityEngine.Random.Range(-positionJitter.y, positionJitter.y));
        float boardRotation = UnityEngine.Random.Range(-maximumRotation, maximumRotation);

        cardView.DrawCardAnimation(boardPosition, boardRotation, onAnimationComplete);
    }
}