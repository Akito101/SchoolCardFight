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

        // 3. Steal Animation Sequence
        Sequence stealSeq = DOTween.Sequence();

        // Pull to board center AND scale up at the same time
        stealSeq.Append(cardTransform.DOMove(boardTransform.position, 0.4f).SetEase(Ease.OutQuad));
        stealSeq.Join(cardTransform.DOScale(Vector3.one * 1.4f, 0.4f).SetEase(Ease.OutQuad));

        // Flip and card face up 
        stealSeq.Append(cardTransform.DOScaleX(0f, 0.3f).SetEase(Ease.InQuad));
        stealSeq.AppendCallback(() => stolenCard.ShowFaceUp());
        stealSeq.Append(cardTransform.DOScaleX(1.5f, 0.3f).SetEase(Ease.OutQuad));

        //  Short pause for better visual effect
        stealSeq.AppendInterval(0.4f);

        // Flip and card face down 
        stealSeq.Append(cardTransform.DOScaleX(0f, 0.3f).SetEase(Ease.InQuad));
        stealSeq.AppendCallback(() => stolenCard.ShowFaceDown());
        stealSeq.Append(cardTransform.DOScaleX(1.5f, 0.3f).SetEase(Ease.OutQuad));

        // Move to artifact owner deck AND scale down to original size
        stealSeq.Append(cardTransform.DOMove(receivingDeckTransform.position, 0.45f).SetEase(Ease.InBack));
        stealSeq.Join(cardTransform.DOScale(1f, 0.3f).SetEase(Ease.InBack));

        // 4. Cleanup and Reshuffle destination deck
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
}