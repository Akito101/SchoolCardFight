using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class PlayerDeckView : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSource deckAudioSource;
    [SerializeField] private AudioClip drawAudioClip;

    [Header("Prefab Settings")]
    [SerializeField] private CardView cardPrefab;

    [Header("Dialogue Settings")]
    [SerializeField] private DialogueController dialogueController;

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
        Sequence seq = DOTween.Sequence();

        // 1: Gather cards to the deck's position first
        seq.Append(AnimationController.Instance.GatherCards(playerDeckView, Vector2.zero));

        // 2: Overhand shuffle right after gathering finishes
        seq.Append(AnimationController.Instance.OverhandShuffle(playerDeckView, Vector2.zero, 100f));

        seq.OnComplete(() =>
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

        int randomIndex = Random.Range(0, list.Count - 4);

        CardData cardCopy = Instantiate(list[randomIndex]);
        cardCopy.isArtifact = true;
        cardCopy.artifactEffect = artifactType;

        list[randomIndex] = cardCopy;
    }

    public CardData DrawCard()
    {
        if (dialogueController != null && dialogueController.IsDialogueActive)
        {
            return null;
        }

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

    private void PlayDrawSound()
    {
        if (deckAudioSource != null && drawAudioClip != null)
        {
            deckAudioSource.PlayOneShot(drawAudioClip);
        }
    }
}