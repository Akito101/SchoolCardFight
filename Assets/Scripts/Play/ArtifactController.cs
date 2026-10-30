using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using System;

public class ArtifactController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private ArtifactUIController artifactUIController;
    [SerializeField] private CardView cardPrefab;

    [Header("Deck References")]
    [SerializeField] private PlayerDeckView playerDeckView;
    [SerializeField] private AIDeckView aiDeckView;
    [SerializeField] private RectTransform boardTransform;

    private int playerProtectionCount = 0;
    private int aiProtectionCount = 0;

    public void ActivateProtection(BoardCardOwner owner)
    {
        if (owner == BoardCardOwner.Player)
        {
            playerProtectionCount++;
        }
        else if (owner == BoardCardOwner.AI)
        {
            aiProtectionCount++;
        }
    }

    public bool ConsumeProtection(BoardCardOwner defender, Action onNotificationComplete = null)
    {
        if (defender == BoardCardOwner.Player && playerProtectionCount > 0)
        {
            playerProtectionCount--;

            if (artifactUIController != null)
            {
                artifactUIController.ShowArtifactNotification(ArtifactEffect.Protection, onNotificationComplete);
            }

            return true;
        }

        if (defender == BoardCardOwner.AI && aiProtectionCount > 0)
        {
            aiProtectionCount--;

            if (artifactUIController != null)
            {
                artifactUIController.ShowArtifactNotification(ArtifactEffect.Protection, onNotificationComplete);
            }
            return true;
        }

        return false;
    }

    public void ResetProtectionOnMatchWinner(BoardCardOwner winner)
    {
        if (winner == BoardCardOwner.Player)
        {
            playerProtectionCount = 0;
        }
        else if (winner == BoardCardOwner.AI)
        {
            aiProtectionCount = 0;
        }
    }

    public void ResetAllProtection()
    {
        playerProtectionCount = 0;
        aiProtectionCount = 0;
    }

    public void ReshuffleOpponentDeck(BoardCardOwner caller, Action onComplete = null)
    {
        if (caller == BoardCardOwner.Player)
        {
            if (aiDeckView == null)
            {
                Debug.LogError("Cannot reshuffle AI deck because the reference is missing.");
                onComplete?.Invoke();
                return;
            }

            aiDeckView.ReshuffleDeck(onComplete);
        }
        else
        {
            if (playerDeckView == null)
            {
                Debug.LogError("Cannot reshuffle player deck because the reference is missing.");
                onComplete?.Invoke();
                return;
            }

            playerDeckView.ReshuffleDeck(onComplete);
        }
    }

    public void StealOpponentCard(BoardCardOwner stealingOwner, Action onComplete = null)
    {
        // 1. If it's the AI's turn, skip the wheel and steal a random card immediately
        if (stealingOwner == BoardCardOwner.AI)
        {
            ExecuteStealLogic(stealingOwner, CardCategory.Humans, onComplete);
            return;
        }

        // 2. If it's the Player's turn, show the wheel UI and wait for category selection
        if (stealingOwner == BoardCardOwner.Player)
        {
            if (artifactUIController != null)
            {
                artifactUIController.ShowStealArtifactWheel((chosenCategory) =>
                {
                    ExecuteStealLogic(stealingOwner, chosenCategory, onComplete);
                });
                return;
            }
        }

        // Fallback
        ExecuteStealLogic(stealingOwner, CardCategory.Humans, onComplete);
    }

    public void ExecuteStealLogic(BoardCardOwner stealingOwner, CardCategory category, Action onComplete = null)
    {
        CardData stolenData;
        RectTransform receivingDeckTransform;
        RectTransform targetDeckTransform;

        if (playerDeckView == null || aiDeckView == null)
        {
            Debug.LogError($"Cannot execute steal operation for {stealingOwner} because one or both deck references (PlayerDeckView / AIDeckView) are missing.");
            onComplete?.Invoke();
            return;
        }

        // 1. Pick random card from opponent deck data
        if (stealingOwner == BoardCardOwner.Player)
        {
            stolenData = aiDeckView.StolenCardByCategory(category);
            targetDeckTransform = aiDeckView.GetComponent<RectTransform>();
            receivingDeckTransform = playerDeckView.GetComponent<RectTransform>();
        }
        else
        {
            stolenData = playerDeckView.StolenCard();
            targetDeckTransform = playerDeckView.GetComponent<RectTransform>();
            receivingDeckTransform = aiDeckView.GetComponent<RectTransform>();
        }

        if (stolenData == null)
        {
            onComplete?.Invoke();
            return;
        }

        // 2. Spawn temporary floating card for animation
        CardView stolenCard = Instantiate(cardPrefab, boardTransform);
        stolenCard.SetCardFront(stolenData.cardFrontSprite);

        RectTransform cardTransform = stolenCard.GetComponent<RectTransform>();
        cardTransform.position = targetDeckTransform.position;
        cardTransform.localScale = Vector3.one;
        cardTransform.SetAsLastSibling();

        List<CardView> singleCardList = new List<CardView> { stolenCard };
        Vector3 boardCenterPos = boardTransform.anchoredPosition;

        // 3. Steal Animation Sequence
        Sequence stealSeq = DOTween.Sequence();

        // Pull to board center AND scale up at the same time
        Sequence pullToCenter = DOTween.Sequence();
        pullToCenter.Join(cardTransform.DOAnchorPos(boardCenterPos, 0.4f).SetEase(Ease.OutQuad));
        pullToCenter.Join(cardTransform.DOScale(Vector3.one * 1.4f, 0.4f).SetEase(Ease.OutQuad));
        stealSeq.Append(pullToCenter);

        stealSeq.Append(AnimationController.Instance.FlipFaceUp(singleCardList, 1.5f));
        stealSeq.AppendInterval(0.4f);
        stealSeq.Append(AnimationController.Instance.FlipFaceDown(singleCardList, 1.5f));

        // Move to artifact owner deck AND scale down to original size
        Sequence deliverSeq = DOTween.Sequence();
        deliverSeq.Join(cardTransform.DOAnchorPos(receivingDeckTransform.anchoredPosition, 0.45f).SetEase(Ease.InBack));
        deliverSeq.Join(cardTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.InBack));
        stealSeq.Append(deliverSeq);

        stealSeq.OnComplete(() =>
        {
            Destroy(stolenCard.gameObject);

            if (stealingOwner == BoardCardOwner.Player)
            {
                playerDeckView.AddCardsAndReshuffle(new List<CardData> { stolenData }, onComplete);
            }
            else
            {
                aiDeckView.AddCardsAndReshuffle(new List<CardData> { stolenData }, onComplete);
            }
        });
    }

    public void TeacherTakeCards(int cardCount, Action onComplete = null)
    {
        if (playerDeckView == null || aiDeckView == null || cardPrefab == null || boardTransform == null)
        {
            Debug.LogError("Cannot process batch steal because a deck, card prefab, or board reference is missing.");
            onComplete?.Invoke();
            return;
        }

        List<CardData> stolenCards = new List<CardData>();
        List<CardView> stolenViews = new List<CardView>();
        RectTransform playerDeckTransform = playerDeckView.GetComponent<RectTransform>();
        RectTransform aiDeckTransform = aiDeckView.GetComponent<RectTransform>();

        int cardsToSteal = Mathf.Min(cardCount, playerDeckView.GetCardCount());
        for (int i = 0; i < cardsToSteal; i++)
        {
            CardData stolenData = playerDeckView.StolenCard();
            if (stolenData == null) break;

            CardView stolenCard = Instantiate(cardPrefab, boardTransform);
            stolenCard.ShowFaceDown();

            RectTransform cardTransform = stolenCard.GetComponent<RectTransform>();
            cardTransform.anchoredPosition = playerDeckTransform.anchoredPosition;
            cardTransform.localScale = Vector3.one;
            cardTransform.SetAsLastSibling();

            stolenCards.Add(stolenData);
            stolenViews.Add(stolenCard);
        }

        if (stolenCards.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        Vector2 boardCenterPos = boardTransform.anchoredPosition;
        float fanSpacing = 34f;
        float fanRotation = 8f;
        Sequence stealSequence = DOTween.Sequence();
        Sequence fanSequence = DOTween.Sequence();
        float fanCenter = (stolenViews.Count - 1) * 0.5f;

        for (int i = 0; i < stolenViews.Count; i++)
        {
            RectTransform cardTransform = stolenViews[i].GetComponent<RectTransform>();
            float fanOffset = i - fanCenter;
            fanSequence.Join(cardTransform.DOAnchorPos(
                boardCenterPos + new Vector2(fanOffset * fanSpacing, Mathf.Abs(fanOffset) * 4f),
                0.4f).SetEase(Ease.OutQuad));
            fanSequence.Join(cardTransform.DOScale(
                1.2f,
                0.4f).SetEase(Ease.OutQuad));
            fanSequence.Join(cardTransform.DORotate(
                new Vector3(0f, 0f, -fanOffset * fanRotation),
                0.4f).SetEase(Ease.OutQuad));
        }

        stealSequence.Append(fanSequence);
        stealSequence.AppendInterval(0.4f);
        stealSequence.Append(AnimationController.Instance.GatherCards(stolenViews, boardCenterPos));

        Sequence deliverSequence = DOTween.Sequence();
        foreach (CardView stolenView in stolenViews)
        {
            if (stolenView == null) continue;

            RectTransform cardTransform = stolenView.GetComponent<RectTransform>();
            deliverSequence.Join(cardTransform.DOAnchorPos(
                aiDeckTransform.anchoredPosition,
                0.45f).SetEase(Ease.InBack));
            deliverSequence.Join(cardTransform.DOScale(Vector3.zero, 0.45f).SetEase(Ease.InBack));
        }

        stealSequence.Append(deliverSequence);
        stealSequence.OnComplete(() =>
        {
            foreach (CardView stolenView in stolenViews)
            {
                if (stolenView != null) Destroy(stolenView.gameObject);
            }

            aiDeckView.AddCardsAndReshuffle(stolenCards, onComplete);
        });
    }

    public void PlayerStealCard(Action onComplete = null)
    {
        if (aiDeckView == null || playerDeckView == null || cardPrefab == null || boardTransform == null)
        {
            Debug.LogError("Cannot steal a Teacher card because a deck, card prefab, or board reference is missing.");
            onComplete?.Invoke();
            return;
        }

        CardData stolenData = aiDeckView.StolenCard();
        if (stolenData == null)
        {
            onComplete?.Invoke();
            return;
        }

        CardView stolenCard = Instantiate(cardPrefab, boardTransform);
        stolenCard.ShowFaceDown();
        stolenCard.SetCardFront(stolenData.cardFrontSprite);

        RectTransform cardTransform = stolenCard.GetComponent<RectTransform>();
        cardTransform.anchoredPosition = aiDeckView.GetComponent<RectTransform>().anchoredPosition;
        cardTransform.localScale = Vector3.one;
        cardTransform.SetAsLastSibling();

        List<CardView> stolenCardList = new List<CardView> { stolenCard };
        Vector3 boardCenterPos = boardTransform.anchoredPosition;

        Sequence seq = DOTween.Sequence();

        Sequence pullToCenter = DOTween.Sequence();
        pullToCenter.Join(cardTransform.DOAnchorPos(boardCenterPos, 0.4f).SetEase(Ease.OutQuad));
        pullToCenter.Join(cardTransform.DOScale(Vector3.one * 1.4f, 0.4f).SetEase(Ease.OutQuad));
        seq.Append(pullToCenter);

        seq.Append(AnimationController.Instance.FlipFaceUp(stolenCardList, 1.5f));
        seq.AppendInterval(0.4f);
        seq.Append(AnimationController.Instance.FlipFaceDown(stolenCardList, 1.5f));

        Sequence deliverSeq = DOTween.Sequence();
        deliverSeq.Join(cardTransform.DOAnchorPos(playerDeckView.GetComponent<RectTransform>().anchoredPosition, 0.45f).SetEase(Ease.InBack));
        deliverSeq.Join(cardTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.InBack));
        seq.Append(deliverSeq);

        seq.AppendCallback(() =>
        {
            Destroy(stolenCard.gameObject);
            playerDeckView.AddCardsWithoutRebuild(
                new List<CardData> { stolenData },
                onComplete);
        });
    }
}