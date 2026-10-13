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

    public void DrawCardAnimation(Vector2 destination, float rotation, Action onComplete)
    {
        currentCardTransform.DOKill(); // stop any previous animation on this object

        Sequence seq = DOTween.Sequence();

        // 1. Move to destination and rotate towards target angle
        seq.Append(currentCardTransform.DOAnchorPos(destination, 0.4f).SetEase(Ease.OutQuad));
        seq.Join(currentCardTransform.DORotate(new Vector3(0f, 0f, rotation), 0.4f).SetEase(Ease.OutQuad));

        // 2. Flip effect: Compress width to 0 (Edge-on)
        seq.Append(currentCardTransform.DOScaleX(0f, 0.2f).SetEase(Ease.InQuad));

        // 3. At the midpoint (when card is invisible/edge-on), swap which side is active!
        seq.AppendCallback(() => ShowFaceUp());

        // 4. Expand back out to full width showing the face
        seq.Append(currentCardTransform.DOScaleX(1f, 0.2f).SetEase(Ease.OutQuad));

        // 5. Landing bounce
        seq.Append(currentCardTransform.DOPunchScale(new Vector3(0.1f, 0.1f, 0f), 0.2f, 4, 0.5f));
        seq.OnComplete(() => onComplete?.Invoke());
    }
}