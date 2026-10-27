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
    private struct BoardCardEntry
    {
        public CardData cardData;
        public BoardCardOwner owner;
        public CardView cardView;
    }

    private readonly List<BoardCardEntry> boardEntries = new List<BoardCardEntry>();

    private Vector2 positionJitter = new Vector2(18f, 12f);
    private RectTransform boardTransform;

    public List<CardData> GetBoardDeck()
    {
        List<CardData> deckCopy = new List<CardData>();
        foreach (var entry in boardEntries)
        {
            deckCopy.Add(entry.cardData);
        }
        return deckCopy;
    }

    private void Awake()
    {
        boardTransform = GetComponent<RectTransform>();
    }

    public void ClearBoard()
    {
        foreach (var entry in boardEntries)
        {
            if (entry.cardView != null) Destroy(entry.cardView.gameObject);
        }

        boardEntries.Clear();
    }

    public void AddCardToBoardDeck(CardData cardData, CardView cardView, BoardCardOwner owner, Action onAnimationComplete)
    {
        if (cardData == null || cardView == null)
        {
            Debug.LogWarning("Cannot add a null card or card view to the board stack");
            return;
        }

        boardEntries.Add(new BoardCardEntry
        {
            cardData = cardData,
            owner = owner,
            cardView = cardView
        });

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
        float cardRotation = UnityEngine.Random.Range(-15f, 15f);

        cardView.DrawCardAnimation(boardPosition, cardRotation, perspectiveScale, onAnimationComplete);
    }

    public void AddBoardDeckToWinnerDeck(Vector2 targetPosition, Action<List<CardData>> onComplete)
    {
        if (boardEntries.Count == 0)
        {
            onComplete?.Invoke(new List<CardData>());
            return;
        }

        List<CardData> wonCards = GetBoardDeck();

        Sequence returnSeq = DOTween.Sequence();
        Vector3 boardCenterPos = boardTransform.anchoredPosition;

        foreach (var entry in boardEntries)
        {
            CardView cardView = entry.cardView;
            if (cardView == null) continue;

            RectTransform cardTrans = cardView.GetComponent<RectTransform>();
            cardTrans.DOKill();

            Sequence cardSeq = DOTween.Sequence();
            cardView.transform.SetAsLastSibling();

            // 1: Gather and straighten all cards into board's center
            cardSeq.Join(cardTrans.DOAnchorPos(boardCenterPos, 0.3f).SetEase(Ease.OutQuad));
            cardSeq.Join(cardTrans.DORotate(Vector3.zero, 0.3f).SetEase(Ease.OutQuad));
            cardSeq.Join(cardTrans.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutQuad));

            // 2: Flip effect: Compress width to 0 (Edge-on)
            cardSeq.Append(cardTrans.DOScaleX(0f, 0.3f).SetEase(Ease.InQuad));
            cardSeq.AppendCallback(() =>
            {
                if (cardView != null) cardView.ShowFaceDown();
            });
            cardSeq.Append(cardTrans.DOScaleX(1f, 0.3f).SetEase(Ease.OutQuad));

            // 3: Short pause for better visual effect
            cardSeq.AppendInterval(0.1f);

            // 4: Move deck to the winner's deck position
            cardSeq.Append(cardTrans.DOAnchorPos(targetPosition, 0.45f).SetEase(Ease.InBack));

            returnSeq.Join(cardSeq);
        }

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
        Vector3 boardCenterPos = boardTransform.anchoredPosition;

        foreach (var entry in boardEntries)
        {
            if (entry.owner == BoardCardOwner.Player)
            {
                playerCards.Add(entry.cardData);
            }
            else
            {
                aiCards.Add(entry.cardData);
            }

            CardView cardView = entry.cardView;
            if (cardView == null) continue;

            Vector2 targetPosition = entry.owner == BoardCardOwner.Player
                ? playerDeckPosition
                : aiDeckPosition;

            RectTransform cardTrans = cardView.GetComponent<RectTransform>();
            cardTrans.DOKill();

            Sequence cardSeq = DOTween.Sequence();
            cardView.transform.SetAsLastSibling();

            // 1: Gather and straighten each decks into respective center positions 
            cardSeq.Join(cardTrans.DOAnchorPos(boardCenterPos, 0.3f).SetEase(Ease.OutQuad));
            cardSeq.Join(cardTrans.DORotate(Vector3.zero, 0.3f).SetEase(Ease.OutQuad));
            cardSeq.Join(cardTrans.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutQuad));

            // 2: Flip effect: Compress width to 0 (Edge-on)
            cardSeq.Append(cardTrans.DOScaleX(0f, 0.3f).SetEase(Ease.InQuad));
            cardSeq.AppendCallback(() => cardView.ShowFaceDown());
            cardSeq.Append(cardTrans.DOScaleX(1f, 0.3f).SetEase(Ease.OutQuad));

            // 3: Move decks to the owner's deck position
            cardSeq.Append(cardTrans.DOAnchorPos(targetPosition, 0.45f).SetEase(Ease.InBack));

            returnSeq.Join(cardSeq);
        }

        returnSeq.OnComplete(() =>
        {
            ClearBoard();
            onComplete?.Invoke(playerCards, aiCards);
        });
    }


}