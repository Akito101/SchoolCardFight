using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class CardView : MonoBehaviour
{
    public Image cardView;
    public RectTransform boardPosition;
    public Sprite cardBackSprite;

    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private CardData currentCard;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
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
        rectTransform.anchoredPosition = originalPosition;
        rectTransform.localScale = Vector3.one; // reset scale in case a previous flip left it mid-shrink

        ShowFaceDown();
    }

    private void ShowFaceDown()
    {
        cardView.sprite = cardBackSprite;
        cardView.color = Color.white;
    }

    private void ShowFaceUp()
    {
        if (currentCard.cardFrontSprite != null)
        {
            cardView.sprite = currentCard.cardFrontSprite;
            cardView.color = Color.white;
        }
        else
        {
            cardView.sprite = null;
            cardView.color = new Color(0.7f, 0.7f, 0.9f);
        }
    }

    public void SetOriginalPosition()
    {
        originalPosition = rectTransform.anchoredPosition;
    }

    public void DrawCardAnimation()
    {
        rectTransform.DOKill(); // stop any previous animation on this object

        Sequence seq = DOTween.Sequence();

        // Move to center while starting the flip
        seq.Append(rectTransform.DOAnchorPos(boardPosition.anchoredPosition, 0.5f).SetEase(Ease.OutQuad));

        // Flip: shrink to edge-on
        seq.Join(rectTransform.DOScaleX(0f, 0.25f).SetEase(Ease.InQuad));

        // At the midpoint, swap the sprite to face-up
        seq.AppendCallback(() => ShowFaceUp());

        // Flip back to full width
        seq.Append(rectTransform.DOScaleX(1f, 0.25f).SetEase(Ease.OutQuad));

        // A little landing bounce for extra juice
        seq.Append(rectTransform.DOPunchScale(new Vector3(0.1f, 0.1f, 0f), 0.2f, 4, 0.5f));
    }
}