using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class AIDeckView : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSource deckAudioSource;
    [SerializeField] private AudioClip shuffleAudioClip;
    [SerializeField] private AudioClip drawAudioClip;

    [Header("Prefab Settings")]
    [SerializeField] private CardView cardPrefab;

    [Header("Dialogue Integration")]
    [SerializeField] private DialogueController aiDialogueController;

    private readonly List<CardData> aiDeckData = new List<CardData>();
    private readonly List<CardView> aiDeckView = new List<CardView>();

    private RectTransform aiDeckTransform;
    private Vector2 stackOffset = new Vector2(-8f, 0f);
    private int maxVisibleLayers = 4;

    public int GetCardCount()
    {
        return aiDeckData.Count;
    }

    void Awake()
    {
        aiDeckTransform = GetComponent<RectTransform>();
    }

    public void BuildDeck(List<CardData> initialDeck, ArtifactEffect artifactType, System.Action onAnimationComplete = null)
    {
        ClearDeck();

        aiDeckData.AddRange(initialDeck);
        ShuffleDeck(aiDeckData);
        InsertArtifact(aiDeckData, artifactType);

        RebuildDeckViews(onAnimationComplete);

        if (aiDialogueController != null)
        {
            aiDialogueController.CheckAIDeckCondition(aiDeckData.Count);
        }
    }

    public void AddCardsAndReshuffle(List<CardData> winedDeck, System.Action onAnimationComplete = null)
    {
        if (winedDeck == null || winedDeck.Count == 0)
        {
            onAnimationComplete?.Invoke();
            return;
        }

        aiDeckData.AddRange(winedDeck);
        ShuffleDeck(aiDeckData);

        RebuildDeckViews(onAnimationComplete);
    }

    private void RebuildDeckViews(System.Action onAnimationComplete)
    {
        // 1. Destroy any existing card views so old card views don't linger
        foreach (CardView card in aiDeckView)
        {
            if (card != null) Destroy(card.gameObject);
        }
        aiDeckView.Clear();

        // 2. Instantiate new views matching the current deck data
        foreach (CardData data in aiDeckData)
        {
            CardView newCard = Instantiate(cardPrefab, aiDeckTransform);
            newCard.ShowFaceDown();
            aiDeckView.Add(newCard);
        }

        AnimateDeckShuffle(onAnimationComplete);
    }

    public void ReshuffleDeck(System.Action onAnimationComplete = null)
    {
        ShuffleDeck(aiDeckData);
        AnimateDeckShuffle(onAnimationComplete);
    }

    private void AnimateDeckShuffle(System.Action onAnimationComplete)
    {
        OverhandDeckShuffleAnimation(aiDeckView, () =>
        {
            RepositionStack();
            onAnimationComplete?.Invoke();
        });
    }

    private void ShuffleDeck<T>(List<T> list)
    {
        // Fisher-Yates shuffle - a standard, reliable way to randomize a list
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
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
        if (aiDeckData.Count == 0) return null;

        PlayDrawSound();

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

        if (aiDialogueController != null)
        {
            aiDialogueController.CheckAIDeckCondition(aiDeckData.Count);
        }

        return drawnCard;
    }

    public CardView SpawnCard()
    {
        CardView newCard = Instantiate(cardPrefab, transform.parent);
        newCard.SetOriginalPosition(aiDeckTransform.anchoredPosition);
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

        for (int i = 0; i < aiDeckView.Count; i++)
        {
            RectTransform cardTrans = aiDeckView[i].GetComponent<RectTransform>();
            int layer = Mathf.Min(aiDeckView.Count - 1 - i, visibleLayerCount - 1);

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
                Vector2 downPosition = new Vector2(0f, -100f);

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

    public CardData StolenCardByCategory(CardCategory targetCategory)
    {
        List<int> matchingIndices = new List<int>();
        for (int i = 0; i < aiDeckData.Count; i++)
        {
            if (aiDeckData[i].category == targetCategory)
            {
                matchingIndices.Add(i);
            }
        }

        // If opponent has no cards of this category, return null (do not steal)
        if (matchingIndices.Count == 0)
        {
            Debug.LogWarning($"No cards of category {targetCategory} found in AI deck.");
            return null;
        }

        PlayDrawSound();

        int randomIndex = matchingIndices[UnityEngine.Random.Range(0, matchingIndices.Count)];
        CardData stolenData = aiDeckData[randomIndex];
        CardView stolenView = aiDeckView[randomIndex];

        aiDeckData.RemoveAt(randomIndex);
        aiDeckView.RemoveAt(randomIndex);

        if (stolenView != null)
        {
            Destroy(stolenView.gameObject);
        }

        RepositionStack();

        if (aiDialogueController != null)
        {
            aiDialogueController.CheckAIDeckCondition(aiDeckData.Count);
        }

        return stolenData;
    }

    private void ClearDeck()
    {
        foreach (CardView card in aiDeckView)
        {
            if (card != null) Destroy(card.gameObject);
        }
        aiDeckView.Clear();
        aiDeckData.Clear();
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