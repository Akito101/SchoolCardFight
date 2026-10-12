using UnityEngine;
using System.Collections.Generic;

public class BoardCardStackView : MonoBehaviour
{
    [SerializeField] private List<CardData> boardCardDeck = new List<CardData>();

    private Vector2 stackOffset = new Vector2(12f, 0f);

    private RectTransform boardTransform;

    private void Awake()
    {
        boardTransform = GetComponent<RectTransform>();
    }

    public void ClearBoard()
    {
        boardCardDeck.Clear();
    }

    public void AddCardToBoardDeck(CardData cardData, CardView cardView)
    {
        if (cardData == null || cardView == null)
        {
            Debug.LogWarning("Cannot add a null card or card view to the board stack");
            return;
        }
        boardCardDeck.Add(cardData);

        cardView.transform.SetAsLastSibling();

        Vector2 boardPosition = boardTransform.anchoredPosition
            + stackOffset * (boardCardDeck.Count - 1);

        cardView.DrawCardAnimation(boardPosition);
    }
}