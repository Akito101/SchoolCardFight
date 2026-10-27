using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class PlayerDeckView : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSource deckAudioSource;
    [SerializeField] private AudioClip shuffleAudioClip;
    [SerializeField] private AudioClip drawAudioClip;

    [Header("Prefab Settings")]
    [SerializeField] private CardView cardPrefab;

    private readonly List<CardData> playerDeckData = new List<CardData>();
    private readonly List<CardView> playerDeckView = new List<CardView>();

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

    public void BuildDeck(List<CardData> initialDeck, ArtifactEffect artifactType, System.Action onAnimationComplete = null)
    {
        ClearDeck();

        playerDeckData.AddRange(initialDeck);
        ShuffleDeck(playerDeckData);
        InsertArtifact(playerDeckData, artifactType);

        RebuildDeckViews(onAnimationComplete);
    }

    public void AddCardsAndReshuffle(List<CardData> winedDeck, System.Action onAnimationComplete = null)
    {
        if (winedDeck == null || winedDeck.Count == 0)
        {
            onAnimationComplete?.Invoke();
            return;
        }

        playerDeckData.AddRange(winedDeck);
        ShuffleDeck(playerDeckData);

        RebuildDeckViews(onAnimationComplete);
    }

    private void RebuildDeckViews(System.Action onAnimationComplete)
    {
        // 1. Destroy any existing card views so old card views don't linger
        foreach (CardView card in playerDeckView)
        {
            if (card != null) Destroy(card.gameObject);
        }
        playerDeckView.Clear();

        // 2. Instantiate new views matching the current deck data
        foreach (CardData data in playerDeckData)
        {
            CardView newCard = Instantiate(cardPrefab, playerDeckTransform);
            newCard.ShowFaceDown();
            playerDeckView.Add(newCard);
        }

        AnimateDeckShuffle(onAnimationComplete);
    }

    public void ReshuffleDeck(System.Action onAnimationComplete = null)
    {
        ShuffleDeck(playerDeckData);
        AnimateDeckShuffle(onAnimationComplete);
    }

    private void AnimateDeckShuffle(System.Action onAnimationComplete)
    {
        OverhandDeckShuffleAnimation(playerDeckView, () =>
        {
            RepositionStack();
            onAnimationComplete?.Invoke();
        });
    }

    private void ShuffleDeck<T>(List<T> list)
    {
        if (list == null || list.Count <= 1) return;

        // Perform Fisher-Yates shuffle 3 times for extra data randomization
        for (int shufflePass = 0; shufflePass < 3; shufflePass++)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
            }
        }
    }

    private void InsertArtifact(List<CardData> list, ArtifactEffect artifactType)
    {
        if (list == null || list.Count < 5) return;

        int randomIndex = Random.Range(4, list.Count);

        CardData cardCopy = Instantiate(list[randomIndex]);
        cardCopy.isArtifact = true;
        cardCopy.artifactEffect = artifactType;

        list[randomIndex] = cardCopy;
    }

    public CardData DrawCard()
    {
        if (playerDeckData.Count == 0) return null;

        PlayDrawSound();

        int topIndex = playerDeckData.Count - 1;

        CardData drawnCard = playerDeckData[topIndex];
        playerDeckData.RemoveAt(topIndex);

        CardView topCardView = playerDeckView[topIndex];
        playerDeckView.RemoveAt(topIndex);

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
        currentCardView.SetCardArtifact(currentCardData);
    }

    private void RepositionStack()
    {
        int visibleLayerCount = Mathf.Max(1, maxVisibleLayers);

        for (int i = 0; i < playerDeckView.Count; i++)
        {
            RectTransform cardTrans = playerDeckView[i].GetComponent<RectTransform>();
            int layer = Mathf.Min(playerDeckView.Count - 1 - i, visibleLayerCount - 1);

            Vector2 targetPosition = stackOffset * layer;
            cardTrans.DOKill();
            cardTrans.DOAnchorPos(targetPosition, 0.2f).SetEase(Ease.OutQuad);
            cardTrans.SetSiblingIndex(i);
        }
    }

    public void OverhandDeckShuffleAnimation(List<CardView> deckCardViews, System.Action onComplete)
    {
        if (deckCardViews == null || deckCardViews.Count < 4)
        {
            onComplete?.Invoke();
            return;
        }

        PlayCardShuffleSound();

        Sequence seq = DOTween.Sequence();

        // 1: Gather and straighten all cards into board's center
        Sequence gatherSeq = DOTween.Sequence();
        foreach (var cardView in deckCardViews)
        {
            if (cardView == null) continue;
            RectTransform cardTrans = cardView.GetComponent<RectTransform>();
            cardTrans.DOKill();
            cardTrans.SetAsLastSibling();

            gatherSeq.Join(cardTrans.DOAnchorPos(Vector3.zero, 0.25f).SetEase(Ease.OutQuad));
            gatherSeq.Join(cardTrans.DORotate(Vector3.zero, 0.25f).SetEase(Ease.OutQuad));
            gatherSeq.Join(cardTrans.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutQuad));
        }
        seq.Append(gatherSeq);

        // 2: Short pause for better visual effect
        seq.AppendInterval(0.1f);

        Sequence shuffleSeq = DOTween.Sequence();
        int shuffleIterations = 3;

        // 3: Overhand shuffle effect
        for (int iter = 0; iter < shuffleIterations; iter++)
        {
            // 3.1: Pick a random chunk from the middle of the deck
            int chunkSize = Random.Range(2, Mathf.Max(3, deckCardViews.Count / 2));
            int startIndex = Random.Range(0, deckCardViews.Count - chunkSize);
            int endIndex = startIndex + chunkSize;

            Sequence iterationSeq = DOTween.Sequence();
            List<RectTransform> movingCards = new List<RectTransform>();

            for (int i = startIndex; i < endIndex; i++)
            {
                if (deckCardViews[i] != null)
                {
                    RectTransform cardTrans = deckCardViews[i].GetComponent<RectTransform>();
                    movingCards.Add(cardTrans);
                }
            }

            if (movingCards.Count == 0) continue;

            // 3.2: Pull down and return up vertically in a single loop per card
            foreach (var cardTrans in movingCards)
            {
                Vector2 downPosition = new Vector2(0f, 100f);

                Sequence cardMoveSeq = DOTween.Sequence();
                cardMoveSeq.Append(cardTrans.DOAnchorPos(downPosition, 0.12f).SetEase(Ease.OutQuad));
                cardMoveSeq.AppendCallback(() => cardTrans.SetAsLastSibling());
                cardMoveSeq.Append(cardTrans.DOAnchorPos(Vector2.zero, 0.12f).SetEase(Ease.InQuad));

                iterationSeq.Join(cardMoveSeq);
            }

            // 3.3: Short pause for better visual effect
            iterationSeq.AppendInterval(0.1f);

            shuffleSeq.Append(iterationSeq);
        }
        seq.Append(shuffleSeq);

        seq.OnComplete(() =>
        {
            onComplete?.Invoke();
        });
    }

    public CardData StolenCard()
    {
        if (playerDeckData.Count == 0) return null;

        PlayDrawSound();

        int randomIndex = Random.Range(0, playerDeckData.Count);
        CardData stolenData = playerDeckData[randomIndex];
        CardView stolenView = playerDeckView[randomIndex];

        playerDeckData.RemoveAt(randomIndex);
        playerDeckView.RemoveAt(randomIndex);

        if (stolenView != null)
        {
            Destroy(stolenView.gameObject);
        }

        RepositionStack();
        return stolenData;
    }

    private void ClearDeck()
    {
        foreach (CardView card in playerDeckView)
        {
            if (card != null) Destroy(card.gameObject);
        }
        playerDeckView.Clear();
        playerDeckData.Clear();
    }

    private void PlayCardShuffleSound()
    {
        if (deckAudioSource != null && shuffleAudioClip != null)
        {
            deckAudioSource.PlayOneShot(shuffleAudioClip);
        }
    }

    private void PlayDrawSound()
    {
        if (deckAudioSource != null && drawAudioClip != null)
        {
            deckAudioSource.PlayOneShot(drawAudioClip);
        }
    }
}