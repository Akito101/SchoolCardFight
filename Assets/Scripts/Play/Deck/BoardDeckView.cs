using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using System;

public enum BoardCardOwner
{
    Player,
    AI
}

public class BoardDeckView : MonoBehaviour
{
    private readonly List<CardData> boardCardDeck = new List<CardData>();
    private readonly List<BoardCardOwner> boardCardOwners = new List<BoardCardOwner>();
    private readonly List<CardView> activeBoardCardViews = new List<CardView>();
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
        boardCardOwners.Clear();

        foreach (var card in activeBoardCardViews)
        {
            if (card != null) Destroy(card.gameObject);
        }

        activeBoardCardViews.Clear();
    }

    public void AddCardToBoardDeck(CardData cardData, CardView cardView, BoardCardOwner owner, Action onAnimationComplete)
    {
        if (cardData == null || cardView == null)
        {
            Debug.LogWarning("Cannot add a null card or card view to the board stack");
            return;
        }

        boardCardDeck.Add(cardData);
        boardCardOwners.Add(owner);
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
        Sequence returnSeq = DOTween.Sequence();

        // 1. Gather and straighten all cards into board's center
        Sequence gatherSeq = DOTween.Sequence();
        Vector3 boardCenterPos = boardTransform.position;

        foreach (var cardView in activeBoardCardViews)
        {
            if (cardView == null) continue;
            RectTransform cardTransform = cardView.GetComponent<RectTransform>();
            cardTransform.DOKill();

            // Bring cards to the front so they stack cleanly on top of each other
            cardView.transform.SetAsLastSibling();

            // Slide to center, reset rotation to straight (0), and reset scale
            gatherSeq.Join(cardTransform.DOMove(boardCenterPos, 0.3f).SetEase(Ease.OutQuad));
            gatherSeq.Join(cardTransform.DORotate(Vector3.zero, 0.3f).SetEase(Ease.OutQuad));
            gatherSeq.Join(cardTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutQuad));
        }
        returnSeq.Append(gatherSeq);

        // 2. Short pause for better visual effect
        returnSeq.AppendInterval(0.1f);

        // 3. Move the entire deck to the winner's deck position
        Sequence moveSeq = DOTween.Sequence();
        foreach (var cardView in activeBoardCardViews)
        {
            if (cardView == null) continue;
            RectTransform cardTransform = cardView.GetComponent<RectTransform>();

            moveSeq.Join(cardTransform.DOAnchorPos(targetPosition, 0.45f).SetEase(Ease.InBack));
        }
        returnSeq.Append(moveSeq);

        returnSeq.OnComplete(() =>
        {
            ClearBoard();
            onComplete?.Invoke(wonCards);
        });
    }

    public void ReturnBoardCardsToOwnerDecks(
    Vector2 playerDeckPosition,
    Vector2 aiDeckPosition,
    Action<List<CardData>, List<CardData>> onComplete)
    {
        List<CardData> playerCards = new List<CardData>();
        List<CardData> aiCards = new List<CardData>();

        Sequence returnSeq = DOTween.Sequence();

        for (int i = 0; i < boardCardDeck.Count; i++)
        {
            CardData cardData = boardCardDeck[i];
            BoardCardOwner owner = boardCardOwners[i];

            if (owner == BoardCardOwner.Player)
            {
                playerCards.Add(cardData);
            }
            else
            {
                aiCards.Add(cardData);
            }

            if (i >= activeBoardCardViews.Count)
            {
                continue;
            }

            CardView cardView = activeBoardCardViews[i];
            if (cardView == null) continue;

            Vector2 targetPosition = owner == BoardCardOwner.Player
                ? playerDeckPosition
                : aiDeckPosition;

            RectTransform cardTransform = cardView.GetComponent<RectTransform>();
            cardTransform.DOKill();

            Sequence rotateSeq = DOTween.Sequence();
            Vector3 boardCenterPos = boardTransform.position;

            // 1: Gather and straighten each decks into respective center positions 
            rotateSeq.Join(cardTransform.DOMove(boardCenterPos, 0.3f).SetEase(Ease.OutQuad));
            rotateSeq.Join(cardTransform.DORotate(Vector3.zero, 0.3f).SetEase(Ease.OutQuad));
            rotateSeq.Join(cardTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutQuad));

            // 2: Flip effect: Compress width to 0 (Edge-on)
            rotateSeq.Append(cardTransform.DOScaleX(0f, 0.3f).SetEase(Ease.InQuad));
            rotateSeq.AppendCallback(() => cardView.ShowFaceDown());
            rotateSeq.Append(cardTransform.DOScaleX(1f, 0.3f).SetEase(Ease.OutQuad));

            // 3: Fly back to the owner's deck position
            rotateSeq.Append(cardTransform.DOAnchorPos(targetPosition, 0.35f).SetEase(Ease.InBack));

            returnSeq.Join(rotateSeq);
        }

        returnSeq.OnComplete(() =>
        {
            ClearBoard();
            onComplete?.Invoke(playerCards, aiCards);
        });
    }
}