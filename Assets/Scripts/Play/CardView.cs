using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

[RequireComponent(typeof(RectTransform))]
public class CardView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject frontSideRoot;
    [SerializeField] private GameObject backSideRoot;
    [SerializeField] private Image frontSideImage;
    [SerializeField] private Image backSideImage;
    [SerializeField] private Sprite cardBackSprite;

    [Header("Artifact Overlays")]
    [SerializeField] private GameObject protectionOverlay;
    [SerializeField] private GameObject reshuffleOverlay;
    [SerializeField] private GameObject stealOverlay;

    private RectTransform currentCardTransform;

    void Awake()
    {
        currentCardTransform = GetComponent<RectTransform>();

        if (backSideImage != null && cardBackSprite != null)
        {
            backSideImage.sprite = cardBackSprite;
        }

        ShowFaceDown();
    }

    public void SetOriginalPosition(Vector2 position)
    {
        currentCardTransform.anchoredPosition = position;
    }

    public void SetCardFront(Sprite frontSprite)
    {
        if (frontSprite == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        currentCardTransform.localScale = Vector3.one;
        currentCardTransform.localRotation = Quaternion.identity;

        if (frontSideImage != null)
        {
            frontSideImage.sprite = frontSprite;
        }

        ShowFaceDown();
    }

    public void SetCardArtifact(CardData cardData)
    {
        if (protectionOverlay) protectionOverlay.SetActive(false);
        if (reshuffleOverlay) reshuffleOverlay.SetActive(false);
        if (stealOverlay) stealOverlay.SetActive(false);

        if (cardData.isArtifact)
        {
            switch (cardData.artifactEffect)
            {
                case ArtifactEffect.Protection:
                    if (protectionOverlay) protectionOverlay.SetActive(true);
                    break;
                case ArtifactEffect.ReshuffleOpponentDeck:
                    if (reshuffleOverlay) reshuffleOverlay.SetActive(true);
                    break;
                case ArtifactEffect.StealCard:
                    if (stealOverlay) stealOverlay.SetActive(true);
                    break;
            }
        }
    }

    public void ShowFaceDown()
    {
        if (backSideRoot != null) backSideRoot.SetActive(true);
        if (frontSideRoot != null) frontSideRoot.SetActive(false);
    }

    public void ShowFaceUp()
    {
        if (backSideRoot != null) backSideRoot.SetActive(false);
        if (frontSideRoot != null) frontSideRoot.SetActive(true);
    }

    public void DrawCardAnimation(
        Vector2 cardDestination,
        float cardRotation,
        float targetScale,
        Action onComplete)
    {
        currentCardTransform.DOKill();

        Sequence seq = DOTween.Sequence();

        // 1. Move to destination and scale up at the same time
        Vector2 startingPos = cardDestination - new Vector2(0f, 190f);
        seq.Append(currentCardTransform.DOAnchorPos(startingPos, 0.4f).SetEase(Ease.OutQuad));
        seq.Join(currentCardTransform.DOScale(Vector3.one * 1.4f, 0.4f).SetEase(Ease.OutQuad));
        seq.Join(currentCardTransform.DORotate(new Vector3(0f, 0f, 0f), 0.4f).SetEase(Ease.OutQuad));

        // 2. Flip effect: Compress width to 0 (Edge-on)
        seq.Append(currentCardTransform.DOScaleX(0f, 0.3f).SetEase(Ease.InQuad));
        seq.AppendCallback(() => ShowFaceUp());
        seq.Append(currentCardTransform.DOScaleX(1.5f, 0.3f).SetEase(Ease.OutQuad));

        // 3. Pause for a split second so the player can look at the revealed card
        seq.AppendInterval(0.6f);

        // 4. Apply the final randomized rotation and scale back down to normal size 
        seq.Append(currentCardTransform.DOAnchorPos(cardDestination, 0.35f).SetEase(Ease.OutQuad));
        seq.Join(currentCardTransform.DOScale(Vector3.one * targetScale, 0.35f).SetEase(Ease.OutQuad));
        seq.Join(currentCardTransform.DORotate(new Vector3(0f, 0f, cardRotation), 0.35f).SetEase(Ease.OutQuad));

        // 5. Landing bounce
        seq.Append(currentCardTransform.DOPunchScale(new Vector3(0.1f, 0.1f, 0f), 0.2f, 4, 0.5f));
        seq.OnComplete(() => onComplete?.Invoke());
    }
}