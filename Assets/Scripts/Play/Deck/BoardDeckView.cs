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
    [Header("Audio Settings")]
    [SerializeField] private AudioSource boardAudioSource;
    [SerializeField] private AudioClip deckClaimAudioClip;

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

    public List<CardData> GetCardsOwnedBy(BoardCardOwner owner)
    {
        List<CardData> cards = new List<CardData>();
        foreach (var entry in boardEntries)
        {
            if (entry.owner == owner)
            {
                cards.Add(entry.cardData);
            }
        }

        return cards;
    }

    public bool TryGetCardOwner(CardData cardData, out BoardCardOwner owner)
    {
        foreach (var entry in boardEntries)
        {
            if (entry.cardData == cardData)
            {
                owner = entry.owner;
                return true;
            }
        }

        owner = BoardCardOwner.Player;
        return false;
    }

    public void ReturnCardsToOwnerDecks(
        List<CardData> playerCardsToReturn,
        List<CardData> aiCardsToReturn,
        Vector2 playerDeckPosition,
        Vector2 aiDeckPosition,
        Action<List<CardData>, List<CardData>> onComplete)
    {
        List<CardData> playerCards = new List<CardData>();
        List<CardData> aiCards = new List<CardData>();
        List<CardView> cardViews = new List<CardView>();
        List<Vector2> targetPositions = new List<Vector2>();

        List<CardData> cardsToReturn = new List<CardData>(playerCardsToReturn);
        cardsToReturn.AddRange(aiCardsToReturn);

        foreach (CardData cardData in cardsToReturn)
        {
            int cardIndex = boardEntries.FindIndex(entry => entry.cardData == cardData);
            if (cardIndex < 0) continue;

            BoardCardEntry entry = boardEntries[cardIndex];
            boardEntries.RemoveAt(cardIndex);

            if (entry.owner == BoardCardOwner.Player)
            {
                playerCards.Add(entry.cardData);
                targetPositions.Add(playerDeckPosition);
            }
            else
            {
                aiCards.Add(entry.cardData);
                targetPositions.Add(aiDeckPosition);
            }

            if (entry.cardView != null) cardViews.Add(entry.cardView);
        }

        if (cardViews.Count == 0)
        {
            onComplete?.Invoke(playerCards, aiCards);
            return;
        }

        PlayReturnSound();
        Vector2 boardCenter = boardTransform.anchoredPosition;
        Sequence returnSequence = DOTween.Sequence();
        returnSequence.Append(AnimationController.Instance.GatherCards(cardViews, boardCenter));
        returnSequence.Append(AnimationController.Instance.FlipFaceDown(cardViews));

        Sequence moveSequence = DOTween.Sequence();
        for (int i = 0; i < cardViews.Count; i++)
        {
            CardView cardView = cardViews[i];
            RectTransform cardTransform = cardView.GetComponent<RectTransform>();
            moveSequence.Join(cardTransform.DOAnchorPos(targetPositions[i], 0.45f).SetEase(Ease.InBack));
        }

        returnSequence.Append(moveSequence);
        returnSequence.OnComplete(() =>
        {
            foreach (CardView cardView in cardViews)
            {
                if (cardView != null) Destroy(cardView.gameObject);
            }

            onComplete?.Invoke(playerCards, aiCards);
        });
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

    public void ReturnCardToOwnerDeck(
        CardData cardData,
        Vector2 targetPosition,
        Action<CardData> onComplete)
    {
        int cardIndex = boardEntries.FindIndex(entry => entry.cardData == cardData);
        if (cardIndex < 0)
        {
            Debug.LogWarning("Cannot return the requested card because it is not on the board.");
            onComplete?.Invoke(null);
            return;
        }

        BoardCardEntry entry = boardEntries[cardIndex];
        boardEntries.RemoveAt(cardIndex);

        if (entry.cardView == null)
        {
            onComplete?.Invoke(entry.cardData);
            return;
        }

        PlayReturnSound();

        CardView cardView = entry.cardView;
        Vector3 boardCenterPos = boardTransform.anchoredPosition;
        Sequence returnSeq = DOTween.Sequence();

        returnSeq.Append(AnimationController.Instance.GatherCards(new List<CardView> { cardView }, boardCenterPos));
        returnSeq.Append(AnimationController.Instance.FlipFaceDown(
            new List<CardView> { cardView }));
        returnSeq.Append(cardView.GetComponent<RectTransform>()
            .DOAnchorPos(targetPosition, 0.45f)
            .SetEase(Ease.InBack));
        returnSeq.OnComplete(() =>
        {
            Destroy(cardView.gameObject);
            onComplete?.Invoke(entry.cardData);
        });
    }

    public void AddBoardDeckToWinnerDeck(Vector2 targetPosition, Action<List<CardData>> onComplete)
    {
        if (boardEntries.Count == 0)
        {
            onComplete?.Invoke(new List<CardData>());
            return;
        }

        PlayReturnSound();

        List<CardData> wonCards = GetBoardDeck();
        List<CardView> cardViews = new List<CardView>();
        foreach (var entry in boardEntries)
        {
            if (entry.cardView != null) cardViews.Add(entry.cardView);
        }

        Vector3 boardCenterPos = boardTransform.anchoredPosition;
        Sequence returnSeq = DOTween.Sequence();

        // 1. Gather all cards to center
        returnSeq.Append(AnimationController.Instance.GatherCards(cardViews, boardCenterPos));

        // 2. Flip face down 
        returnSeq.Append(AnimationController.Instance.FlipFaceDown(cardViews));
        returnSeq.AppendInterval(0.1f);

        // 3. Move all cards to winner's deck position
        Sequence moveSeq = DOTween.Sequence();
        foreach (var cardView in cardViews)
        {
            if (cardView == null) continue;
            RectTransform cardTrans = cardView.GetComponent<RectTransform>();
            cardTrans.DOKill();
            moveSeq.Join(cardTrans.DOAnchorPos(targetPosition, 0.45f).SetEase(Ease.InBack));
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
        PlayReturnSound();

        List<CardData> playerCards = new List<CardData>();
        List<CardData> aiCards = new List<CardData>();
        List<CardView> cardViews = new List<CardView>();

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

            if (entry.cardView != null)
            {
                cardViews.Add(entry.cardView);
            }
        }

        if (cardViews.Count == 0)
        {
            ClearBoard();
            onComplete?.Invoke(playerCards, aiCards);
            return;
        }

        Vector3 boardCenterPos = boardTransform.anchoredPosition;
        Sequence returnSeq = DOTween.Sequence();

        // 1. Gather all cards to center
        returnSeq.Append(AnimationController.Instance.GatherCards(cardViews, boardCenterPos));

        // 2. Flip face down
        returnSeq.Append(AnimationController.Instance.FlipFaceDown(cardViews));
        returnSeq.AppendInterval(0.1f);

        // 3. Move each card to its specific owner's deck position
        Sequence moveSeq = DOTween.Sequence();
        foreach (var entry in boardEntries)
        {
            CardView cardView = entry.cardView;
            if (cardView == null) continue;

            Vector2 targetPosition = (entry.owner == BoardCardOwner.Player) ? playerDeckPosition : aiDeckPosition;
            RectTransform cardTrans = cardView.GetComponent<RectTransform>();
            cardTrans.DOKill();

            moveSeq.Join(cardTrans.DOAnchorPos(targetPosition, 0.45f).SetEase(Ease.InBack));
        }
        returnSeq.Append(moveSeq);

        returnSeq.OnComplete(() =>
        {
            ClearBoard();
            onComplete?.Invoke(playerCards, aiCards);
        });
    }

    public void MixAndDistributeBoardCards(Vector2 playerDeckPosition,
    Vector2 aiDeckPosition,
    Action<List<CardData>, List<CardData>> onComplete)
    {
        // 1: Combine collection gathering into a single efficient pass
        List<CardData> allCards = new List<CardData>();
        List<CardView> cardViews = new List<CardView>();

        foreach (var entry in boardEntries)
        {
            if (entry.cardData != null) allCards.Add(entry.cardData);
            if (entry.cardView != null) cardViews.Add(entry.cardView);
        }

        if (allCards.Count == 0 || cardViews.Count == 0)
        {
            ClearBoard();
            onComplete?.Invoke(new List<CardData>(), new List<CardData>());
            return;
        }

        // 2: Shuffle all the card data randomly
        for (int i = allCards.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            (allCards[i], allCards[randomIndex]) = (allCards[randomIndex], allCards[i]);
        }

        // 3: Split the shuffled card data evenly into two halves
        int halfCount = allCards.Count / 2;
        List<CardData> playerCards = new List<CardData>();
        List<CardData> aiCards = new List<CardData>();

        for (int i = 0; i < allCards.Count; i++)
        {
            if (i < halfCount) playerCards.Add(allCards[i]);
            else aiCards.Add(allCards[i]);
        }

        // 4: Animation sequence to gather, flip, shuffle, and distribute cards
        Vector3 boardCenterPos = boardTransform.anchoredPosition;
        Sequence mainSeq = DOTween.Sequence();

        mainSeq.Append(AnimationController.Instance.GatherCards(cardViews, boardCenterPos, PlayReturnSound));
        mainSeq.Append(AnimationController.Instance.FlipFaceDown(cardViews));
        mainSeq.Append(AnimationController.Instance.OverhandShuffle(cardViews, boardCenterPos, 100f));

        Sequence distributeSeq = DOTween.Sequence();


        int index = 0;
        foreach (var entry in boardEntries)
        {
            CardView cardView = entry.cardView;
            if (cardView == null) continue;

            Vector2 targetPosition = (index < halfCount) ? playerDeckPosition : aiDeckPosition;
            index++;

            RectTransform cardTrans = cardView.GetComponent<RectTransform>();
            distributeSeq.Join(cardTrans.DOAnchorPos(targetPosition, 0.45f).SetEase(Ease.InBack));
        }
        mainSeq.Append(distributeSeq);

        mainSeq.OnComplete(() =>
        {
            ClearBoard();
            onComplete?.Invoke(playerCards, aiCards);
        });
    }

    private void PlayReturnSound()
    {
        if (boardAudioSource != null && deckClaimAudioClip != null)
        {
            boardAudioSource.PlayOneShot(deckClaimAudioClip);
        }
    }
}