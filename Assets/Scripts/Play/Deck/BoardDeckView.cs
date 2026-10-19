using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using System;

public class BoardDeckView : MonoBehaviour
{
    [SerializeField] private List<CardData> boardCardDeck = new List<CardData>();
    private List<CardView> activeBoardCardViews = new List<CardView>();
    private Vector2 positionJitter = new Vector2(18f, 12f);
    private float maximumRotation = 15f;
    private RectTransform boardTransform;

    public List<CardData> GetBoardDeck() => new List<CardData>(boardCardDeck);

    private void Awake()
    {
        boardTransform = GetComponent<RectTransform>();
    }

    public void ClearBoard()
    {
        boardCardDeck.Clear();
        foreach (var card in activeBoardCardViews)
        {
            if (card != null) Destroy(card.gameObject);
        }
        activeBoardCardViews.Clear();
    }

    public void AddCardToBoardDeck(CardData cardData, CardView cardView, Action onAnimationComplete)
    {
        if (cardData == null || cardView == null)
        {
            Debug.LogWarning("Cannot add a null card or card view to the board stack");
            return;
        }

        boardCardDeck.Add(cardData);
        activeBoardCardViews.Add(cardView);
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

    public void AddBoardDeckToWinnerDeck(Vector2 targetPosition, Action<List<CardData>> onComplete)
    {
        if (activeBoardCardViews.Count == 0)
        {
            List<CardData> empty = new List<CardData>(boardCardDeck);
            ClearBoard();
            onComplete?.Invoke(empty);
            return;
        }

        List<CardData> wonCards = new List<CardData>(boardCardDeck);
        Sequence mainSeq = DOTween.Sequence();

        // 1. Gather and straighten all cards into a neat stack at the board's center
        Sequence gatherSeq = DOTween.Sequence();
        Vector3 boardCenterPos = boardTransform.position;

        foreach (var cardView in activeBoardCardViews)
        {
            if (cardView == null) continue;
            RectTransform rt = cardView.GetComponent<RectTransform>();
            rt.DOKill();

            // Bring cards to the front so they stack cleanly on top of each other
            cardView.transform.SetAsLastSibling();

            // Slide to center, reset rotation to straight (0), and reset scale
            gatherSeq.Join(rt.DOMove(boardCenterPos, 0.3f).SetEase(Ease.OutQuad));
            gatherSeq.Join(rt.DORotate(Vector3.zero, 0.3f).SetEase(Ease.OutQuad));
            gatherSeq.Join(rt.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutQuad));
        }
        mainSeq.Append(gatherSeq);

        // 2. short pause for better visual effect before moving to the winner's deck
        mainSeq.AppendInterval(0.1f);

        // 3. Move the entire unified stack together to the winner's deck position
        Sequence moveSeq = DOTween.Sequence();
        foreach (var cardView in activeBoardCardViews)
        {
            if (cardView == null) continue;
            RectTransform rt = cardView.GetComponent<RectTransform>();

            moveSeq.Join(rt.DOAnchorPos(targetPosition, 0.45f).SetEase(Ease.InBack));
        }
        mainSeq.Append(moveSeq);

        // Clean up and finish when everything is done
        mainSeq.OnComplete(() =>
        {
            ClearBoard();
            onComplete?.Invoke(wonCards);
        });
    }
}