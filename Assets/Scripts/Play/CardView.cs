using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

[RequireComponent(typeof(RectTransform))]
public class CardView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image frontSideImage;
    [SerializeField] private Image backSideImage;
    [SerializeField] private Sprite cardBackSprite;

    private RectTransform currentCardTransform;
    private Vector2 playerDeckPosition;
    private CardData currentCard;

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
        playerDeckPosition = position;
        currentCardTransform.anchoredPosition = position;
    }

    public CardView CreateNextDrawCard()
    {
        CardView nextCard = Instantiate(this, transform.parent);
        nextCard.playerDeckPosition = playerDeckPosition;
        nextCard.currentCardTransform.anchoredPosition = playerDeckPosition;
        nextCard.currentCardTransform.localRotation = Quaternion.identity;
        return nextCard;
    }

    public CardData GetCurrentCardData()
    {
        return currentCard;
    }

    public void Setup(CardData data)
    {
        if (data == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        currentCard = data;
        currentCardTransform.anchoredPosition = playerDeckPosition;
        currentCardTransform.localScale = Vector3.one;
        currentCardTransform.localRotation = Quaternion.identity;

        if (frontSideImage != null && currentCard.cardFrontSprite != null)
        {
            frontSideImage.sprite = currentCard.cardFrontSprite;
        }

        ShowFaceDown();
    }

    private void ShowFaceDown()
    {
        if (backSideImage != null) backSideImage.gameObject.SetActive(true);
        if (frontSideImage != null) frontSideImage.gameObject.SetActive(false);
    }

    private void ShowFaceUp()
    {
        if (backSideImage != null) backSideImage.gameObject.SetActive(false);
        if (frontSideImage != null) frontSideImage.gameObject.SetActive(true);
    }

    public void DrawCardAnimation(Vector2 cardDestination, float cardRotation, float targetScale, Action onComplete)
    {
        currentCardTransform.DOKill(); // stop any previous animation on this object

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